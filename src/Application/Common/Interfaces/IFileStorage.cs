using LanguageExt;

namespace Application.Common.Interfaces;

public interface IFileStorage
{
    Task<Unit> UploadAsync(Stream stream, string fileFullPath, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string fileFullPath, CancellationToken cancellationToken);
    Task<string> GetFileUrlAsync(string fileFullPath, CancellationToken cancellationToken);
}