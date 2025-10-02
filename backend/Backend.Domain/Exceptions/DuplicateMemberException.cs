namespace Backend.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to add a user who is already a member of an organization.
/// </summary>
public class DuplicateMemberException : DomainException
{
    public Guid UserId { get; }
    public Guid OrganizationId { get; }

    public DuplicateMemberException(Guid userId, Guid organizationId)
        : base($"User '{userId}' is already a member of organization '{organizationId}'.")
    {
        UserId = userId;
        OrganizationId = organizationId;
    }
}
