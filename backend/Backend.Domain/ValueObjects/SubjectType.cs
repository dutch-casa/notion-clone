namespace Backend.Domain.ValueObjects;

/// <summary>
/// SubjectType value object defining who can be granted access.
/// Part of Authorization bounded context.
/// </summary>
public readonly struct SubjectType : IEquatable<SubjectType>
{
    public string Value { get; }

    private SubjectType(string value)
    {
        Value = value;
    }

    public static SubjectType Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Invalid subject type", nameof(input));
        }

        var normalized = input.Trim().ToLowerInvariant();
        if (normalized != "user" && normalized != "org" && normalized != "role")
        {
            throw new ArgumentException($"Invalid subject type: {input}", nameof(input));
        }

        return new SubjectType(normalized);
    }

    public static SubjectType User => new("user");
    public static SubjectType Org => new("org");
    public static SubjectType Role => new("role");

    public bool Equals(SubjectType other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is SubjectType type && Equals(type);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(SubjectType left, SubjectType right) => left.Equals(right);
    public static bool operator !=(SubjectType left, SubjectType right) => !(left == right);

    public static implicit operator string(SubjectType type) => type.Value;

    public override string ToString() => Value;
}
