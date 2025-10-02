namespace Backend.Domain.Aggregates;

/// <summary>
/// Image aggregate root representing an uploaded image file.
/// Part of Documents bounded context.
/// </summary>
public class Image
{
    public Guid Id { get; private set; }
    public Guid PageId { get; private set; }
    public Guid OrgId { get; private set; }
    public string FileName { get; private set; }
    public string FileKey { get; private set; } // S3/MinIO path key
    public string ContentType { get; private set; }
    public long FileSizeBytes { get; private set; }
    public Guid UploadedBy { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }

    public Image(Guid pageId, Guid orgId, string fileName, string fileKey, string contentType, long fileSizeBytes, Guid uploadedBy)
    {
        if (pageId == Guid.Empty)
        {
            throw new ArgumentException("PageId cannot be empty", nameof(pageId));
        }

        if (orgId == Guid.Empty)
        {
            throw new ArgumentException("OrgId cannot be empty", nameof(orgId));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("FileName cannot be empty", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(fileKey))
        {
            throw new ArgumentException("FileKey cannot be empty", nameof(fileKey));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("ContentType cannot be empty", nameof(contentType));
        }

        if (fileSizeBytes <= 0)
        {
            throw new ArgumentException("FileSizeBytes must be greater than zero", nameof(fileSizeBytes));
        }

        if (uploadedBy == Guid.Empty)
        {
            throw new ArgumentException("UploadedBy cannot be empty", nameof(uploadedBy));
        }

        Id = Guid.NewGuid();
        PageId = pageId;
        OrgId = orgId;
        FileName = fileName.Trim();
        FileKey = fileKey.Trim();
        ContentType = contentType.Trim();
        FileSizeBytes = fileSizeBytes;
        UploadedBy = uploadedBy;
        UploadedAt = DateTimeOffset.UtcNow;
    }

    // EF Core constructor
    private Image() { }
}
