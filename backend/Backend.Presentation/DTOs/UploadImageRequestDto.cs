namespace Backend.Presentation.DTOs;

public class UploadImageRequestDto
{
    public Guid PageId { get; set; }
    public Guid OrgId { get; set; }
    public IFormFile File { get; set; } = null!;
}
