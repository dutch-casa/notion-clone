using Backend.Domain.ValueObjects;

namespace Backend.Domain.Entities;

/// <summary>
/// Invitation entity representing an invitation to join an organization.
/// Part of IdentityOrg bounded context.
/// </summary>
public class Invitation
{
    public Guid Id { get; private set; }
    public Guid OrgId { get; private set; }
    public Guid InvitedUserId { get; private set; }
    public Guid InviterUserId { get; private set; }
    public OrgRole Role { get; private set; }
    public InvitationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }

    public Invitation(Guid orgId, Guid invitedUserId, Guid inviterUserId, OrgRole role)
    {
        if (orgId == Guid.Empty)
        {
            throw new ArgumentException("OrgId cannot be empty", nameof(orgId));
        }

        if (invitedUserId == Guid.Empty)
        {
            throw new ArgumentException("InvitedUserId cannot be empty", nameof(invitedUserId));
        }

        if (inviterUserId == Guid.Empty)
        {
            throw new ArgumentException("InviterUserId cannot be empty", nameof(inviterUserId));
        }

        Id = Guid.NewGuid();
        OrgId = orgId;
        InvitedUserId = invitedUserId;
        InviterUserId = inviterUserId;
        Role = role;
        Status = InvitationStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Accept()
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot accept invitation with status {Status}");
        }

        Status = InvitationStatus.Accepted;
        RespondedAt = DateTimeOffset.UtcNow;
    }

    public void Decline()
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot decline invitation with status {Status}");
        }

        Status = InvitationStatus.Declined;
        RespondedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot cancel invitation with status {Status}");
        }

        Status = InvitationStatus.Cancelled;
        RespondedAt = DateTimeOffset.UtcNow;
    }

    // EF Core constructor
    private Invitation() { }
}

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Cancelled = 3
}
