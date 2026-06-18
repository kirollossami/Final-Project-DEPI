namespace Business.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string subFolder);
    Task<(byte[] Content, string ContentType)?> GetFileAsync(string relativePath);
    Task<bool> DeleteFileAsync(string relativePath);
}
