namespace Backend.Application.UseCases.Images.UploadImage;

public record UploadImageCommand(
    Guid PageId,
    Guid OrgId,
    string FileName,
    Stream FileStream,
    string ContentType,
    long FileSizeBytes,
    Guid UserId
);
