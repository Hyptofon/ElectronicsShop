using Application.Common.Interfaces;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Infrastructure.FileStorage;

public class LocalFileStorageService(ILogger<LocalFileStorageService> logger) : IFileStorage
{
    private const string BaseDirectory = "uploads";

    public async Task<Unit> UploadAsync(Stream stream, string fileFullPath, CancellationToken cancellationToken)
    {
        try
        {
            var fullPath = Path.Combine(BaseDirectory, fileFullPath);
            
            var normalizedFullPath = Path.GetFullPath(fullPath);
            var normalizedBaseDirectory = Path.GetFullPath(BaseDirectory);
            
            if (!normalizedFullPath.StartsWith(normalizedBaseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Invalid file path detected: {Path}", fileFullPath);
                throw new InvalidOperationException("Invalid file path");
            }

            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fileStream, cancellationToken);

            logger.LogInformation("File uploaded successfully: {FilePath}", fullPath);

            return Unit.Default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading file: {FilePath}", fileFullPath);
            throw;
        }
    }

    public Task<bool> DeleteAsync(string fileFullPath, CancellationToken cancellationToken)
    {
        try
        {
            var fullPath = Path.Combine(BaseDirectory, fileFullPath);
            
            var normalizedFullPath = Path.GetFullPath(fullPath);
            var normalizedBaseDirectory = Path.GetFullPath(BaseDirectory);
            
            if (!normalizedFullPath.StartsWith(normalizedBaseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Invalid file path detected: {Path}", fileFullPath);
                return Task.FromResult(false);
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                logger.LogInformation("File deleted successfully: {FilePath}", fullPath);
                return Task.FromResult(true);
            }

            logger.LogWarning("File not found for deletion: {FilePath}", fullPath);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting file: {FilePath}", fileFullPath);
            throw;
        }
    }

    public Task<string> GetFileUrlAsync(string fileFullPath, CancellationToken cancellationToken)
    {
        var url = $"/{BaseDirectory}/{fileFullPath.Replace("\\", "/")}";
        return Task.FromResult(url);
    }
}