using Dapper;
using backend.Application.Interfaces;
using backend.Domain.Entities;

namespace backend.Infrastructure.Repositories;

public class FileRepository : IFileRepository
{
    private readonly IDbSession _dbSession;

    public FileRepository(IDbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<Guid> CreateFileAsync(FileEntity file)
    {
        var sql = @"
            INSERT INTO files (id, workspace_id, title, folder_path, created_by, created_at, updated_at, deleted_at)
            VALUES (@Id, @WorkspaceId, @Title, @FolderPath, @CreatedBy, @CreatedAt, @UpdatedAt, @DeletedAt)
            RETURNING id;";
        return await _dbSession.Connection.ExecuteScalarAsync<Guid>(sql, file, _dbSession.Transaction);
    }

    public async Task<FileEntity?> GetFileByIdAsync(Guid fileId)
    {
        var sql = @"
            SELECT id, workspace_id as ""WorkspaceId"", title as ""Title"", folder_path as ""FolderPath"", 
                   created_by as ""CreatedBy"", created_at as ""CreatedAt"", updated_at as ""UpdatedAt"", deleted_at as ""DeletedAt""
            FROM files WHERE id = @FileId AND deleted_at IS NULL";
        return await _dbSession.Connection.QuerySingleOrDefaultAsync<FileEntity>(sql, new { FileId = fileId }, _dbSession.Transaction);
    }

    public async Task<IEnumerable<FileEntity>> GetFilesByWorkspaceIdAsync(Guid workspaceId, int limit = 100, int offset = 0)
    {
        var sql = @"
            SELECT id, workspace_id as ""WorkspaceId"", title as ""Title"", folder_path as ""FolderPath"", 
                   created_by as ""CreatedBy"", created_at as ""CreatedAt"", updated_at as ""UpdatedAt""
            FROM files 
            WHERE workspace_id = @WorkspaceId AND deleted_at IS NULL
            ORDER BY created_at DESC
            LIMIT @Limit OFFSET @Offset";
        return await _dbSession.Connection.QueryAsync<FileEntity>(sql, new { WorkspaceId = workspaceId, Limit = limit, Offset = offset }, _dbSession.Transaction);
    }

    public async Task<IEnumerable<FileEntity>> GetTrashFilesAsync(Guid workspaceId, int limit = 100, int offset = 0)
    {
        var sql = @"
            SELECT id, workspace_id as ""WorkspaceId"", title as ""Title"", folder_path as ""FolderPath"", 
                   created_by as ""CreatedBy"", created_at as ""CreatedAt"", updated_at as ""UpdatedAt"", deleted_at as ""DeletedAt""
            FROM files 
            WHERE workspace_id = @WorkspaceId AND deleted_at IS NOT NULL
            ORDER BY deleted_at DESC
            LIMIT @Limit OFFSET @Offset";
        return await _dbSession.Connection.QueryAsync<FileEntity>(sql, new { WorkspaceId = workspaceId, Limit = limit, Offset = offset }, _dbSession.Transaction);
    }

    public async Task<bool> SoftDeleteFileAsync(Guid fileId)
    {
        var sql = "UPDATE files SET deleted_at = NOW() WHERE id = @FileId AND deleted_at IS NULL";
        var rows = await _dbSession.Connection.ExecuteAsync(sql, new { FileId = fileId }, _dbSession.Transaction);
        return rows > 0;
    }

    public async Task<bool> RestoreFileAsync(Guid fileId)
    {
        var sql = "UPDATE files SET deleted_at = NULL WHERE id = @FileId AND deleted_at IS NOT NULL";
        var rows = await _dbSession.Connection.ExecuteAsync(sql, new { FileId = fileId }, _dbSession.Transaction);
        return rows > 0;
    }

    public async Task<bool> HardDeleteFileAsync(Guid fileId)
    {
        var sql = "DELETE FROM files WHERE id = @FileId";
        var rows = await _dbSession.Connection.ExecuteAsync(sql, new { FileId = fileId }, _dbSession.Transaction);
        return rows > 0;
    }

    public async Task<IEnumerable<backend.Application.DTOs.File.FileSearchResultDto>> SearchFilesAsync(Guid workspaceId, string keyword)
    {
        var sql = @"
            SELECT id as ""Id"", workspace_id as ""WorkspaceId"", title as ""Title"", folder_path as ""FolderPath"", 
                   created_by as ""CreatedBy"", created_at as ""CreatedAt"", updated_at as ""UpdatedAt""
            FROM files
            WHERE workspace_id = @WorkspaceId 
              AND deleted_at IS NULL
              AND (
                  unaccent(title) ILIKE '%' || unaccent(@Keyword) || '%'
                  OR search_vector @@ plainto_tsquery('simple', unaccent(@Keyword))
              )
            ORDER BY 
              (unaccent(title) ILIKE '%' || unaccent(@Keyword) || '%') DESC,
              ts_rank(search_vector, plainto_tsquery('simple', unaccent(@Keyword))) DESC,
              updated_at DESC
            LIMIT 50;";
        return await _dbSession.Connection.QueryAsync<backend.Application.DTOs.File.FileSearchResultDto>(
            sql, 
            new { WorkspaceId = workspaceId, Keyword = keyword }, 
            _dbSession.Transaction);
    }

    public async Task UpdateFileSearchVectorAsync(Guid fileId, string rawText)
    {
        var sql = @"
            UPDATE files 
            SET search_vector = to_tsvector('simple', unaccent(@RawText)) 
            WHERE id = @FileId;";
        await _dbSession.Connection.ExecuteAsync(sql, new { FileId = fileId, RawText = rawText }, _dbSession.Transaction);
    }

    public async Task<Guid> CreateVersionAsync(VersionEntity version)
    {
        var sql = @"
            INSERT INTO versions (id, file_id, version_number, storage_path, is_full, base_version_id, file_size, checksum, created_by, created_at, expired_at)
            VALUES (@Id, @FileId, @VersionNumber, @StoragePath, @IsFull, @BaseVersionId, @FileSize, @Checksum, @CreatedBy, @CreatedAt, @ExpiredAt)
            RETURNING id;";
        return await _dbSession.Connection.ExecuteScalarAsync<Guid>(sql, version, _dbSession.Transaction);
    }

    public async Task<VersionEntity?> GetVersionByIdAsync(Guid versionId)
    {
        var sql = @"
            SELECT v.id, v.file_id as ""FileId"", v.version_number as ""VersionNumber"", v.storage_path as ""StoragePath"", 
                   v.is_full as ""IsFull"", v.base_version_id as ""BaseVersionId"", v.file_size as ""FileSize"", 
                   v.checksum as ""Checksum"", v.created_by as ""CreatedBy"", v.created_at as ""CreatedAt"", v.expired_at as ""ExpiredAt""
            FROM versions v
            INNER JOIN files f ON v.file_id = f.id
            WHERE v.id = @VersionId AND f.deleted_at IS NULL";
        return await _dbSession.Connection.QuerySingleOrDefaultAsync<VersionEntity>(sql, new { VersionId = versionId }, _dbSession.Transaction);
    }

    public async Task<IEnumerable<VersionEntity>> GetVersionsByFileIdAsync(Guid fileId, int limit = 100, int offset = 0)
    {
        var sql = @"
            SELECT v.id, v.file_id as ""FileId"", v.version_number as ""VersionNumber"", v.storage_path as ""StoragePath"", 
                   v.is_full as ""IsFull"", v.base_version_id as ""BaseVersionId"", v.file_size as ""FileSize"", 
                   v.checksum as ""Checksum"", v.created_by as ""CreatedBy"", v.created_at as ""CreatedAt"", v.expired_at as ""ExpiredAt""
            FROM versions v
            INNER JOIN files f ON v.file_id = f.id
            WHERE v.file_id = @FileId AND f.deleted_at IS NULL
            ORDER BY v.version_number DESC
            LIMIT @Limit OFFSET @Offset";
        return await _dbSession.Connection.QueryAsync<VersionEntity>(sql, new { FileId = fileId, Limit = limit, Offset = offset }, _dbSession.Transaction);
    }

    public async Task<VersionEntity?> GetLatestVersionAsync(Guid fileId)
    {
        var sql = @"
            SELECT v.id, v.file_id as ""FileId"", v.version_number as ""VersionNumber"", v.storage_path as ""StoragePath"", 
                   v.is_full as ""IsFull"", v.base_version_id as ""BaseVersionId"", v.file_size as ""FileSize"", 
                   v.checksum as ""Checksum"", v.created_by as ""CreatedBy"", v.created_at as ""CreatedAt"", v.expired_at as ""ExpiredAt""
            FROM versions v
            INNER JOIN files f ON v.file_id = f.id
            WHERE v.file_id = @FileId AND f.deleted_at IS NULL
            ORDER BY v.version_number DESC LIMIT 1";
        return await _dbSession.Connection.QuerySingleOrDefaultAsync<VersionEntity>(sql, new { FileId = fileId }, _dbSession.Transaction);
    }

    public async Task<VersionEntity?> GetNearestFullBaseAsync(Guid fileId, int currentVersionNumber)
    {
        var sql = @"
            SELECT v.id, v.file_id as ""FileId"", v.version_number as ""VersionNumber"", v.storage_path as ""StoragePath"", 
                   v.is_full as ""IsFull"", v.base_version_id as ""BaseVersionId"", v.file_size as ""FileSize"", 
                   v.checksum as ""Checksum"", v.created_by as ""CreatedBy"", v.created_at as ""CreatedAt"", v.expired_at as ""ExpiredAt""
            FROM versions v
            INNER JOIN files f ON v.file_id = f.id
            WHERE v.file_id = @FileId 
              AND v.is_full = true 
              AND v.version_number <= @CurrentVersionNumber 
              AND f.deleted_at IS NULL
            ORDER BY v.version_number DESC 
            LIMIT 1";
        return await _dbSession.Connection.QuerySingleOrDefaultAsync<VersionEntity>(sql, new { FileId = fileId, CurrentVersionNumber = currentVersionNumber }, _dbSession.Transaction);
    }

    public async Task<IEnumerable<VersionEntity>> GetVersionsBetweenAsync(Guid fileId, int startVersionNumber, int endVersionNumber)
    {
        var sql = @"
            SELECT v.id, v.file_id as ""FileId"", v.version_number as ""VersionNumber"", v.storage_path as ""StoragePath"", 
                   v.is_full as ""IsFull"", v.base_version_id as ""BaseVersionId"", v.file_size as ""FileSize"", 
                   v.checksum as ""Checksum"", v.created_by as ""CreatedBy"", v.created_at as ""CreatedAt"", v.expired_at as ""ExpiredAt""
            FROM versions v
            INNER JOIN files f ON v.file_id = f.id
            WHERE v.file_id = @FileId 
              AND v.version_number >= @StartVersionNumber 
              AND v.version_number <= @EndVersionNumber
              AND f.deleted_at IS NULL
            ORDER BY v.version_number ASC";
        return await _dbSession.Connection.QueryAsync<VersionEntity>(sql, new { FileId = fileId, StartVersionNumber = startVersionNumber, EndVersionNumber = endVersionNumber }, _dbSession.Transaction);
    }

    public async Task<int> MarkExpiredVersionsAsync(Guid fileId, int keepCount)
    {
        var sql = @"
            WITH ranked_versions AS (
                SELECT id, version_number
                FROM versions
                WHERE file_id = @FileId
                ORDER BY version_number DESC
                OFFSET @KeepCount
            )
            UPDATE versions
            SET expired_at = NOW()
            WHERE file_id = @FileId
              AND version_number IN (SELECT version_number FROM ranked_versions)
              AND expired_at IS NULL;";
        return await _dbSession.Connection.ExecuteAsync(sql, new { FileId = fileId, KeepCount = keepCount }, _dbSession.Transaction);
    }
}
