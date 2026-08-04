namespace FluxGrid.Api.Shared.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    public string BasePath { get; }
    private readonly string _baseUrl;

    public LocalFileStorageService(IConfiguration config)
    {
        BasePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(BasePath);
        _baseUrl = config["Urls"]?.TrimEnd('/') ?? "http://localhost:5020";
    }

    public Task<string> GeneratePresignedUploadUrlAsync(string bucketName, string objectKey, string contentType, int expiryMinutes = 5)
    {
        var dir = Path.Combine(BasePath, bucketName, Path.GetDirectoryName(objectKey) ?? "");
        Directory.CreateDirectory(dir);
        var url = $"{_baseUrl}/api/v1/hr/storage/{bucketName}/{objectKey}";
        return Task.FromResult(url);
    }

    public Task<string> GeneratePresignedDownloadUrlAsync(string bucketName, string objectKey, int expiryHours = 1)
    {
        var url = $"{_baseUrl}/api/v1/hr/storage/{bucketName}/{objectKey}";
        return Task.FromResult(url);
    }

    public async Task<byte[]> ReadFileAsync(string bucketName, string objectKey)
    {
        var path = Path.Combine(BasePath, bucketName, objectKey);
        return await File.ReadAllBytesAsync(path);
    }

    public Task DeleteFileAsync(string bucketName, string objectKey)
    {
        var path = Path.Combine(BasePath, bucketName, objectKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public string GetFilePath(string bucketName, string objectKey)
    {
        return Path.Combine(BasePath, bucketName, objectKey);
    }
}
