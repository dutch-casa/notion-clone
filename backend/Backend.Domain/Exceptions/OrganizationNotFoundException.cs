namespace Backend.Domain.Exceptions;

/// <summary>
/// Exception thrown when an organization cannot be found.
/// </summary>
public class OrganizationNotFoundException : EntityNotFoundException
{
    public OrganizationNotFoundException(Guid organizationId)
        : base("Organization", organizationId)
    {
    }
}
