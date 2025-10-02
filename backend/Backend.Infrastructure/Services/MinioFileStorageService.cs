using Backend.Application.Services;
using Minio;
using Minio.DataModel.Args;

namespace Backend.Infrastructure.Services;

/// <summary>
/// MinIO implementation of file storage service.
/// </summary>
public class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private bool _bucketChecked = false;
    private readonly SemaphoreSlim _bucketCheckLock = new SemaphoreSlim(1, 1);

    public MinioFileStorageService(IMinioClient minioClient, string bucketName)
    {
        _minioClient = minioClient ?? throw new ArgumentNullException(nameof(minioClient));
        _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default)
    {
        if (_bucketChecked) return;

        await _bucketCheckLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketChecked) return;

            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(_bucketName);

            bool found = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

            if (!found)
            {
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(_bucketName);

                await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
            }

            _bucketChecked = true;
        }
        finally
        {
            _bucketCheckLock.Release();
        }
    }

    public async Task<string> UploadFileAsync(string fileKey, Stream stream, string contentType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
        {
            throw new ArgumentException("File key cannot be empty", nameof(fileKey));
        }

        if (stream == null || stream.Length == 0)
        {
            throw new ArgumentException("Stream cannot be null or empty", nameof(stream));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type cannot be empty", nameof(contentType));
        }

        try
        {
            // Ensure bucket exists
            await EnsureBucketExistsAsync(cancellationToken);

            // Ensure the stream is at the beginning
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileKey)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

            // Return the public URL
            // MinIO default endpoint is localhost:9000 in development
            // In production, this should come from configuration
            return $"http://localhost:9000/{_bucketName}/{fileKey}";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to upload file '{fileKey}' to MinIO", ex);
        }
    }

    public async Task DeleteFileAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
        {
            throw new ArgumentException("File key cannot be empty", nameof(fileKey));
        }

        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileKey);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete file '{fileKey}' from MinIO", ex);
        }
    }

    public async Task<string> GetPresignedUrlAsync(string fileKey, int expirySeconds = 86400, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
        {
            throw new ArgumentException("File key cannot be empty", nameof(fileKey));
        }

        if (expirySeconds <= 0)
        {
            throw new ArgumentException("Expiry seconds must be greater than zero", nameof(expirySeconds));
        }

        try
        {
            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileKey)
                .WithExpiry(expirySeconds);

            var url = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
            return url;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate presigned URL for file '{fileKey}'", ex);
        }
    }
}
