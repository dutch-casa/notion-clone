using Backend.Application.Services;
using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Images.DeleteImage;

public class DeleteImageHandler
{
    private readonly IImageRepository _imageRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteImageHandler(
        IImageRepository imageRepository,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork)
    {
        _imageRepository = imageRepository;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(DeleteImageCommand command)
    {
        // Find the image
        var image = await _imageRepository.GetByIdAsync(command.ImageId);

        if (image == null)
        {
            throw new InvalidOperationException($"Image {command.ImageId} not found");
        }

        // Delete from storage
        await _fileStorageService.DeleteFileAsync(image.FileKey);

        // Delete from database
        _imageRepository.Remove(image);
        await _unitOfWork.SaveChangesAsync();
    }
}
