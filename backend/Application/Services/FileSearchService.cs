using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Tasks;
using backend.Application.DTOs.File;
using backend.Application.Interfaces;
using StackExchange.Redis;

namespace backend.Application.Services
{
    public class FileSearchService : IFileSearchService
    {
        private readonly IFileRepository _fileRepository;
        private readonly IConnectionMultiplexer _redis;

        public FileSearchService(IFileRepository fileRepository, IConnectionMultiplexer redis)
        {
            _fileRepository = fileRepository;
            _redis = redis;
        }

        private string NormalizeKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return "";
            var cleaned = Regex.Replace(keyword, @"\s+", " ").Trim().ToLowerInvariant();
            return cleaned;
        }

        public async Task<List<FileSearchResultDto>> SearchAsync(Guid workspaceId, string keyword)
        {
            var normalized = NormalizeKeyword(keyword);
            if (string.IsNullOrEmpty(normalized)) return new List<FileSearchResultDto>();

            if (normalized.Length < 2) return new List<FileSearchResultDto>(); // Too short

            var db = _redis.GetDatabase();
            var cacheKey = $"search:ws:{workspaceId}:q:{normalized}";

            var cached = await db.StringGetAsync(cacheKey);
            if (cached.HasValue)
            {
                return JsonSerializer.Deserialize<List<FileSearchResultDto>>(cached.ToString()!) ?? new List<FileSearchResultDto>();
            }

            var results = await _fileRepository.SearchFilesAsync(workspaceId, normalized);
            var resultList = new List<FileSearchResultDto>(results);

            await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(resultList), TimeSpan.FromMinutes(5));

            return resultList;
        }

        public async Task InvalidateWorkspaceSearchCacheAsync(Guid workspaceId)
        {
            var db = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints()[0]);
            var pattern = $"search:ws:{workspaceId}:*";
            
            // Note: Keys method shouldn't be used in large production environment, 
            // SCAN is preferred, StackExchange.Redis automatically uses SCAN under the hood for Keys.
            foreach (var key in server.Keys(pattern: pattern))
            {
                await db.KeyDeleteAsync(key);
            }
        }
    }
}