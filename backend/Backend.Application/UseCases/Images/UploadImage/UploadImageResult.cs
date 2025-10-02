namespace Backend.Application.UseCases.Images.UploadImage;

public record UploadImageResult(
    Guid Id,
    Guid PageId,
    Guid OrgId,
    string FileName,
    string FileUrl,
    string ContentType,
    long FileSizeBytes,
    Guid UploadedBy,
    DateTimeOffset UploadedAt
);
