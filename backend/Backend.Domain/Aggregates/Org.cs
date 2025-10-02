using Backend.Domain.Common;
using Backend.Domain.Entities;
using Backend.Domain.Events;
using Backend.Domain.ValueObjects;

namespace Backend.Domain.Aggregates;

/// <summary>
/// Org aggregate root representing an organization with members.
/// Part of IdentityOrg bounded context.
/// </summary>
public class Org : AggregateRoot
{
    private List<Member> _members = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // EF Core needs access - expose as ICollection for EF, but use backing field for domain logic
    public ICollection<Member> Members => _members;

    public Org(string name, Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty", nameof(name));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("OwnerId cannot be empty", nameof(ownerId));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        OwnerId = ownerId;
        CreatedAt = DateTimeOffset.UtcNow;

        // Add owner as first member
        var ownerMember = new Member(Id, ownerId, OrgRole.Owner);
        _members.Add(ownerMember);
    }

    public Member AddMember(Guid userId, OrgRole role)
    {
        if (_members.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException($"User {userId} is already a member of this organization");
        }

        var member = new Member(Id, userId, role);
        _members.Add(member);

        // Raise domain event
        AddDomainEvent(new MemberAddedToOrganizationEvent(Id, userId, role.Value));

        return member;
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == OwnerId)
        {
            throw new InvalidOperationException("Cannot remove owner from organization");
        }

        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
        {
            throw new InvalidOperationException($"User {userId} is not a member of this organization");
        }

        _members.Remove(member);
    }

    public void UpdateMemberRole(Guid userId, OrgRole newRole)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
        {
            throw new InvalidOperationException($"User {userId} is not a member of this organization");
        }

        if (userId == OwnerId)
        {
            throw new InvalidOperationException("The owner role cannot be changed");
        }

        member.UpdateRole(newRole);
    }

    public Member? GetMember(Guid userId)
    {
        return _members.FirstOrDefault(m => m.UserId == userId);
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Name cannot be empty", nameof(newName));
        }

        Name = newName.Trim();
    }

    // EF Core constructor
    private Org()
    {
        // EF Core will populate these
        Name = string.Empty;
    }
}
