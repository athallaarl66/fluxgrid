using Minio;
using Minio.DataModel.Args;
using Minio.DataModel;
using System.IO;

namespace FluxGrid.Api.Shared.Infrastructure.Storage;

public class S3FileStorageService : IFileStorageService
{
    private readonly IMinioClient _client;
    private readonly bool _useSsl;

    public S3FileStorageService(IMinioClient client, bool useSsl)
    {
        _client = client;
        _useSsl = useSsl;
    }

    public async Task<string> GeneratePresignedUploadUrlAsync(string bucketName, string objectKey, string contentType, int expiryMinutes = 5)
    {
        var args = new PresignedPutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectKey)
            .WithExpiry(expiryMinutes * 60);

        return await _client.PresignedPutObjectAsync(args);
    }

    public async Task<string> GeneratePresignedDownloadUrlAsync(string bucketName, string objectKey, int expiryHours = 1)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectKey)
            .WithExpiry(expiryHours * 3600);

        return await _client.PresignedGetObjectAsync(args);
    }

    public async Task<byte[]> ReadFileAsync(string bucketName, string objectKey)
    {
        using var ms = new MemoryStream();
        var args = new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectKey)
            .WithCallbackStream(stream => stream.CopyTo(ms));
        await _client.GetObjectAsync(args);
        return ms.ToArray();
    }

    public async Task DeleteFileAsync(string bucketName, string objectKey)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectKey);

        await _client.RemoveObjectAsync(args);
    }
}
