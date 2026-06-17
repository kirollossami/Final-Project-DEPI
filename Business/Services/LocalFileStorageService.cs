using Business.Interfaces;

namespace Business.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService()
    {
        _basePath = Path.Combine(AppContext.BaseDirectory, "uploads");
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string subFolder)
    {
        var folderPath = Path.Combine(_basePath, subFolder);

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);

        await using var output = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(output);

        return $"{subFolder}/{fileName}";
    }

    public async Task<(byte[] Content, string ContentType)?> GetFileAsync(string relativePath)
    {
        if (IsPathTraversal(relativePath))
            return null;

        var fullPath = Path.Combine(_basePath, relativePath);

        if (!File.Exists(fullPath))
            return null;

        var content = await File.ReadAllBytesAsync(fullPath);
        var contentType = GetContentType(fullPath);

        return (content, contentType);
    }

    public Task<bool> DeleteFileAsync(string relativePath)
    {
        if (IsPathTraversal(relativePath))
            return Task.FromResult(false);

        var fullPath = Path.Combine(_basePath, relativePath);

        if (!File.Exists(fullPath))
            return Task.FromResult(false);

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    private static bool IsPathTraversal(string path)
    {
        return path.Contains("..", StringComparison.Ordinal);
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
