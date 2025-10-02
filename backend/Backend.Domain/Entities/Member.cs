using Backend.Domain.ValueObjects;

namespace Backend.Domain.Entities;

/// <summary>
/// Member entity representing a user's membership in an organization.
/// Part of IdentityOrg bounded context.
/// </summary>
public class Member
{
    public Guid Id { get; private set; }
    public Guid OrgId { get; private set; }
    public Guid UserId { get; private set; }
    public OrgRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    public Member(Guid orgId, Guid userId, OrgRole role)
    {
        if (orgId == Guid.Empty)
        {
            throw new ArgumentException("OrgId cannot be empty", nameof(orgId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        }

        Id = Guid.NewGuid();
        OrgId = orgId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateRole(OrgRole newRole)
    {
        Role = newRole;
    }

    // EF Core constructor
    private Member() { }
}
