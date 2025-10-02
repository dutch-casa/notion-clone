using Backend.Application.Services;
using Backend.Domain.Aggregates;
using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Images.UploadImage;

public class UploadImageHandler
{
    private readonly IPageRepository _pageRepository;
    private readonly IImageRepository _imageRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public UploadImageHandler(
        IPageRepository pageRepository,
        IImageRepository imageRepository,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork)
    {
        _pageRepository = pageRepository;
        _imageRepository = imageRepository;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<UploadImageResult> HandleAsync(UploadImageCommand command)
    {
        // Verify page exists and user has access to organization
        var page = await _pageRepository.GetByIdAsync(command.PageId);
        if (page == null)
        {
            throw new InvalidOperationException($"Page {command.PageId} not found");
        }

        // Verify page belongs to the organization
        if (page.OrgId != command.OrgId)
        {
            throw new InvalidOperationException($"Page {command.PageId} does not belong to organization {command.OrgId}");
        }

        // Generate unique file key: {orgId}/{pageId}/{timestamp}_{filename}
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var fileKey = $"{command.OrgId}/{command.PageId}/{timestamp}_{command.FileName}";

        // Upload file to storage
        var fileUrl = await _fileStorageService.UploadFileAsync(
            fileKey,
            command.FileStream,
            command.ContentType
        );

        // Create image aggregate
        var image = new Image(
            command.PageId,
            command.OrgId,
            command.FileName,
            fileKey,
            command.ContentType,
            command.FileSizeBytes,
            command.UserId
        );

        await _imageRepository.AddAsync(image);
        await _unitOfWork.SaveChangesAsync();

        return new UploadImageResult(
            image.Id,
            image.PageId,
            image.OrgId,
            image.FileName,
            fileUrl,
            image.ContentType,
            image.FileSizeBytes,
            image.UploadedBy,
            image.UploadedAt
        );
    }
}
