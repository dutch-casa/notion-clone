namespace Backend.Domain.Exceptions;

/// <summary>
/// Exception thrown when an invitation already exists for a user/org combination.
/// </summary>
public class InvitationAlreadyExistsException : DomainException
{
    public Guid UserId { get; }
    public Guid OrganizationId { get; }

    public InvitationAlreadyExistsException(Guid userId, Guid organizationId)
        : base($"An invitation already exists for user '{userId}' to organization '{organizationId}'.")
    {
        UserId = userId;
        OrganizationId = organizationId;
    }
}
