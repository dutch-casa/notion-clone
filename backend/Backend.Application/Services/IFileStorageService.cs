namespace Backend.Application.Services;

/// <summary>
/// Service for storing and retrieving files from object storage (e.g., MinIO, S3).
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to object storage.
    /// </summary>
    /// <param name="fileKey">The unique key/path for the file in the bucket</param>
    /// <param name="stream">The file content stream</param>
    /// <param name="contentType">The MIME type of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The public URL of the uploaded file</returns>
    Task<string> UploadFileAsync(string fileKey, Stream stream, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from object storage.
    /// </summary>
    /// <param name="fileKey">The unique key/path for the file in the bucket</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteFileAsync(string fileKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a presigned URL for a file (useful for private buckets).
    /// </summary>
    /// <param name="fileKey">The unique key/path for the file in the bucket</param>
    /// <param name="expirySeconds">How long the URL should be valid (default: 24 hours)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A presigned URL that can be used to access the file</returns>
    Task<string> GetPresignedUrlAsync(string fileKey, int expirySeconds = 86400, CancellationToken cancellationToken = default);
}
