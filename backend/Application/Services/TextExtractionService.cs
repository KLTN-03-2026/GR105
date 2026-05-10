using System;
using System.IO;
using System.Threading.Tasks;
using backend.Application.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using System.Text;
using System.Text.RegularExpressions;

namespace backend.Application.Services
{
    public class TextExtractionService : ITextExtractionService
    {
        public async Task<string?> ExtractTextAsync(string filePath, string contentType, string fileName)
        {
            if (!File.Exists(filePath)) return null;

            var fileInfo = new FileInfo(filePath);
            // Limit extraction to 5MB
            if (fileInfo.Length > 5 * 1024 * 1024) return null;

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            
            try
            {
                if (ext == ".docx" || contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                {
                    return await Task.Run(() => ExtractFromDocx(filePath));
                }
                
                var isText = contentType.StartsWith("text/") || ext == ".txt" || ext == ".md" || ext == ".csv" || ext == ".json";
                if (isText)
                {
                    var rawText = await File.ReadAllTextAsync(filePath);
                    return CleanText(rawText);
                }
            }
            catch
            {
                // Fallback gracefully without breaking main flow
                return null;
            }

            return null;
        }

        private string ExtractFromDocx(string filePath)
        {
            var sb = new StringBuilder();
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDoc.MainDocumentPart?.Document.Body;
                if (body != null)
                {
                    sb.Append(body.InnerText);
                }
            }
            return CleanText(sb.ToString());
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            // Replace multiple whitespace/newlines with single space
            return Regex.Replace(text, @"\s+", " ").Trim();
        }
    }
}