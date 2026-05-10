using System;
using System.Threading.Tasks;

namespace backend.Application.Interfaces
{
    public interface ITextExtractionService
    {
        Task<string?> ExtractTextAsync(string filePath, string contentType, string fileName);
    }
}