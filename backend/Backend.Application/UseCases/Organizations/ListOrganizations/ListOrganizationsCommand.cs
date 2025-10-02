namespace Backend.Application.UseCases.Organizations.ListOrganizations;

public record ListOrganizationsCommand(Guid UserId);

public record OrganizationDto(
    Guid Id,
    string Name,
    Guid OwnerId,
    DateTimeOffset CreatedAt,
    string Role);
