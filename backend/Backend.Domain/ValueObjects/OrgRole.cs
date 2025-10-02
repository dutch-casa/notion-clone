namespace Backend.Domain.ValueObjects;

/// <summary>
/// Organization role value object representing member permissions.
/// Supports: owner, admin, member.
/// </summary>
public readonly struct OrgRole : IEquatable<OrgRole>
{
    private const string OwnerValue = "owner";
    private const string AdminValue = "admin";
    private const string MemberValue = "member";

    public string Value { get; }

    private OrgRole(string value)
    {
        Value = value;
    }

    public static OrgRole Create(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role cannot be empty", nameof(role));
        }

        var normalized = role.Trim().ToLowerInvariant();

        return normalized switch
        {
            OwnerValue => new OrgRole(OwnerValue),
            AdminValue => new OrgRole(AdminValue),
            MemberValue => new OrgRole(MemberValue),
            _ => throw new ArgumentException($"Invalid role: {role}. Must be owner, admin, or member.", nameof(role))
        };
    }

    // Pre-defined roles
    public static OrgRole Owner => new(OwnerValue);
    public static OrgRole Admin => new(AdminValue);
    public static OrgRole Member => new(MemberValue);

    // Role checks
    public bool IsOwner => Value == OwnerValue;
    public bool IsAdmin => Value == AdminValue;
    public bool IsMember => Value == MemberValue;

    // Permission checks
    public bool CanInviteMembers => IsOwner || IsAdmin;
    public bool CanManageOrg => IsOwner;

    public override string ToString() => Value;

    public bool Equals(OrgRole other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is OrgRole role && Equals(role);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(OrgRole left, OrgRole right) => left.Equals(right);

    public static bool operator !=(OrgRole left, OrgRole right) => !(left == right);

    public static implicit operator string(OrgRole role) => role.Value;
}
