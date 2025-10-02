namespace Backend.Application.UseCases.Organizations.CreateOrganization;

public record CreateOrganizationCommand(string Name, Guid OwnerId);

public record CreateOrganizationResult(Guid OrgId, string Name, Guid OwnerId, DateTimeOffset CreatedAt);
