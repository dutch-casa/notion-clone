using Backend.Application.Services;
using Backend.Application.UseCases.Images.UploadImage;
using Backend.Application.UseCases.Images.DeleteImage;
using Backend.Application.UseCases.Organizations.CreateOrganization;
using Backend.Application.UseCases.Pages.CreatePage;
using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;
using Backend.Tests.Application;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Application.UseCases.Images;

public class ImageHandlersTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public ImageHandlersTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UploadImage_ShouldPersistImageToDatabase()
    {
        // Arrange - Create user, org, and page
        using var dbContext = _fixture.CreateNewContext();

        var user = new User(Email.Create($"image-{Guid.NewGuid()}@example.com"), "Image User", "hash");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var userRepository = new Backend.Infrastructure.Persistence.Repositories.UserRepository(dbContext);
        var orgRepositoryForCreate = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var unitOfWorkForCreate = _fixture.CreateUnitOfWork(dbContext);
        var createOrgHandler = new CreateOrganizationHandler(userRepository, orgRepositoryForCreate, unitOfWorkForCreate);
        var orgResult = await createOrgHandler.HandleAsync(
            new CreateOrganizationCommand("Image Org", user.Id));

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var pageRepository = new Backend.Infrastructure.Persistence.Repositories.PageRepository(dbContext);
        var unitOfWork = _fixture.CreateUnitOfWork(dbContext);
        var createPageHandler = new CreatePageHandler(orgRepository, pageRepository, unitOfWork);
        var pageResult = await createPageHandler.HandleAsync(
            new CreatePageCommand(orgResult.OrgId, "Image Page", user.Id));

        // Mock file storage service
        var mockFileStorageService = new MockFileStorageService();

        // Create test image stream
        var testImageContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header bytes
        var imageStream = new MemoryStream(testImageContent);

        // Act - Upload image
        var imageRepository = new Backend.Infrastructure.Persistence.Repositories.ImageRepository(dbContext);
        var uploadHandler = new UploadImageHandler(pageRepository, imageRepository, mockFileStorageService, unitOfWork);
        var uploadCommand = new UploadImageCommand(
            pageResult.Id,
            orgResult.OrgId,
            "test-image.png",
            imageStream,
            "image/png",
            testImageContent.Length,
            user.Id);

        var result = await uploadHandler.HandleAsync(uploadCommand);

        // Assert - Image uploaded successfully
        result.Should().NotBeNull();
        result.FileName.Should().Be("test-image.png");
        result.ContentType.Should().Be("image/png");
        result.FileSizeBytes.Should().Be(testImageContent.Length);
        result.PageId.Should().Be(pageResult.Id);
        result.OrgId.Should().Be(orgResult.OrgId);
        result.UploadedBy.Should().Be(user.Id);
        result.FileUrl.Should().Contain("test-image.png");

        // Verify image persisted to database
        var savedImage = await dbContext.Images.FindAsync(result.Id);
        savedImage.Should().NotBeNull();
        savedImage!.FileName.Should().Be("test-image.png");
        savedImage.PageId.Should().Be(pageResult.Id);
    }

    [Fact]
    public async Task UploadImage_ShouldThrowWhenPageNotFound()
    {
        // Arrange
        using var dbContext = _fixture.CreateNewContext();

        var user = new User(Email.Create($"nopage-{Guid.NewGuid()}@example.com"), "No Page User", "hash");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var mockFileStorageService = new MockFileStorageService();
        var testImageContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var imageStream = new MemoryStream(testImageContent);

        // Act & Assert - Should throw when page doesn't exist
        var pageRepository = new Backend.Infrastructure.Persistence.Repositories.PageRepository(dbContext);
        var imageRepository = new Backend.Infrastructure.Persistence.Repositories.ImageRepository(dbContext);
        var unitOfWork = _fixture.CreateUnitOfWork(dbContext);
        var uploadHandler = new UploadImageHandler(pageRepository, imageRepository, mockFileStorageService, unitOfWork);
        var uploadCommand = new UploadImageCommand(
            Guid.NewGuid(), // Non-existent page
            Guid.NewGuid(),
            "test-image.png",
            imageStream,
            "image/png",
            testImageContent.Length,
            user.Id);

        var act = async () => await uploadHandler.HandleAsync(uploadCommand);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UploadImage_ShouldThrowWhenPageDoesNotBelongToOrganization()
    {
        // Arrange - Create two different orgs and a page in one org
        using var dbContext = _fixture.CreateNewContext();

        var user = new User(Email.Create($"wrongorg-{Guid.NewGuid()}@example.com"), "Wrong Org User", "hash");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var userRepository = new Backend.Infrastructure.Persistence.Repositories.UserRepository(dbContext);
        var orgRepositoryForCreate = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var unitOfWorkForCreate = _fixture.CreateUnitOfWork(dbContext);
        var createOrgHandler = new CreateOrganizationHandler(userRepository, orgRepositoryForCreate, unitOfWorkForCreate);
        var org1Result = await createOrgHandler.HandleAsync(
            new CreateOrganizationCommand("Org 1", user.Id));
        var org2Result = await createOrgHandler.HandleAsync(
            new CreateOrganizationCommand("Org 2", user.Id));

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var pageRepository = new Backend.Infrastructure.Persistence.Repositories.PageRepository(dbContext);
        var unitOfWork = _fixture.CreateUnitOfWork(dbContext);
        var createPageHandler = new CreatePageHandler(orgRepository, pageRepository, unitOfWork);
        var pageResult = await createPageHandler.HandleAsync(
            new CreatePageCommand(org1Result.OrgId, "Page in Org 1", user.Id));

        var mockFileStorageService = new MockFileStorageService();
        var testImageContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var imageStream = new MemoryStream(testImageContent);

        // Act & Assert - Should throw when trying to upload to page with wrong org ID
        var imageRepository = new Backend.Infrastructure.Persistence.Repositories.ImageRepository(dbContext);
        var uploadHandler = new UploadImageHandler(pageRepository, imageRepository, mockFileStorageService, unitOfWork);
        var uploadCommand = new UploadImageCommand(
            pageResult.Id,
            org2Result.OrgId, // Wrong org ID
            "test-image.png",
            imageStream,
            "image/png",
            testImageContent.Length,
            user.Id);

        var act = async () => await uploadHandler.HandleAsync(uploadCommand);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not belong to organization*");
    }

    [Fact]
    public async Task DeleteImage_ShouldRemoveImageFromDatabase()
    {
        // Arrange - Create user, org, page, and upload an image
        using var dbContext = _fixture.CreateNewContext();

        var user = new User(Email.Create($"delete-{Guid.NewGuid()}@example.com"), "Delete User", "hash");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var userRepository = new Backend.Infrastructure.Persistence.Repositories.UserRepository(dbContext);
        var orgRepositoryForCreate = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var unitOfWorkForCreate = _fixture.CreateUnitOfWork(dbContext);
        var createOrgHandler = new CreateOrganizationHandler(userRepository, orgRepositoryForCreate, unitOfWorkForCreate);
        var orgResult = await createOrgHandler.HandleAsync(
            new CreateOrganizationCommand("Delete Org", user.Id));

        var orgRepository = new Backend.Infrastructure.Persistence.Repositories.OrgRepository(dbContext);
        var pageRepository = new Backend.Infrastructure.Persistence.Repositories.PageRepository(dbContext);
        var unitOfWork = _fixture.CreateUnitOfWork(dbContext);
        var createPageHandler = new CreatePageHandler(orgRepository, pageRepository, unitOfWork);
        var pageResult = await createPageHandler.HandleAsync(
            new CreatePageCommand(orgResult.OrgId, "Delete Page", user.Id));

        var mockFileStorageService = new MockFileStorageService();
        var testImageContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var imageStream = new MemoryStream(testImageContent);

        var imageRepository = new Backend.Infrastructure.Persistence.Repositories.ImageRepository(dbContext);
        var uploadHandler = new UploadImageHandler(pageRepository, imageRepository, mockFileStorageService, unitOfWork);
        var uploadResult = await uploadHandler.HandleAsync(
            new UploadImageCommand(
                pageResult.Id,
                orgResult.OrgId,
                "delete-me.png",
                imageStream,
                "image/png",
                testImageContent.Length,
                user.Id));

        // Act - Delete the image
        var deleteHandler = new DeleteImageHandler(imageRepository, mockFileStorageService, unitOfWork);
        var deleteCommand = new DeleteImageCommand(uploadResult.Id, user.Id);
        await deleteHandler.HandleAsync(deleteCommand);

        // Assert - Image removed from database
        var deletedImage = await dbContext.Images.FindAsync(uploadResult.Id);
        deletedImage.Should().BeNull();

        // Assert - File was deleted from storage (FileKey is the full path, not just filename)
        mockFileStorageService.DeletedFileKeys.Should().HaveCount(1);
        mockFileStorageService.DeletedFileKeys[0].Should().Contain("delete-me.png");
    }

    [Fact]
    public async Task DeleteImage_ShouldThrowWhenImageNotFound()
    {
        // Arrange
        using var dbContext = _fixture.CreateNewContext();

        var user = new User(Email.Create($"notfound-{Guid.NewGuid()}@example.com"), "Not Found User", "hash");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var mockFileStorageService = new MockFileStorageService();

        // Act & Assert - Should throw when image doesn't exist
        var imageRepository = new Backend.Infrastructure.Persistence.Repositories.ImageRepository(dbContext);
        var unitOfWork = _fixture.CreateUnitOfWork(dbContext);
        var deleteHandler = new DeleteImageHandler(imageRepository, mockFileStorageService, unitOfWork);
        var deleteCommand = new DeleteImageCommand(Guid.NewGuid(), user.Id);

        var act = async () => await deleteHandler.HandleAsync(deleteCommand);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    /// <summary>
    /// Mock file storage service for testing
    /// </summary>
    private class MockFileStorageService : IFileStorageService
    {
        public List<string> UploadedFileKeys { get; } = new();
        public List<string> DeletedFileKeys { get; } = new();

        public Task<string> UploadFileAsync(string fileKey, Stream stream, string contentType, CancellationToken cancellationToken = default)
        {
            UploadedFileKeys.Add(fileKey);
            return Task.FromResult($"http://localhost:9000/test-bucket/{fileKey}");
        }

        public Task DeleteFileAsync(string fileKey, CancellationToken cancellationToken = default)
        {
            DeletedFileKeys.Add(fileKey);
            return Task.CompletedTask;
        }

        public Task<string> GetPresignedUrlAsync(string fileKey, int expirySeconds = 86400, CancellationToken cancellationToken = default)
        {
            return Task.FromResult($"http://localhost:9000/test-bucket/{fileKey}?presigned=true");
        }
    }
}
