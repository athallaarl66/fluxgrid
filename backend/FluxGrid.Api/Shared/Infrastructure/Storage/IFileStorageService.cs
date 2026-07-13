namespace FluxGrid.Api.Shared.Infrastructure.Storage;

public interface IFileStorageService
{
    Task<string> GeneratePresignedUploadUrlAsync(string bucketName, string objectKey, string contentType, int expiryMinutes = 5);
    Task<string> GeneratePresignedDownloadUrlAsync(string bucketName, string objectKey, int expiryHours = 1);
    Task DeleteFileAsync(string bucketName, string objectKey);
    Task<byte[]> ReadFileAsync(string bucketName, string objectKey);
}
