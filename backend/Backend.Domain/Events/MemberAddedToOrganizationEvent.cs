namespace Backend.Domain.Events;

/// <summary>
/// Domain event raised when a member is added to an organization.
/// </summary>
public class MemberAddedToOrganizationEvent : IDomainEvent
{
    public Guid OrganizationId { get; }
    public Guid UserId { get; }
    public string Role { get; }
    public DateTime OccurredAt { get; }

    public MemberAddedToOrganizationEvent(Guid organizationId, Guid userId, string role)
    {
        OrganizationId = organizationId;
        UserId = userId;
        Role = role;
        OccurredAt = DateTime.UtcNow;
    }
}
