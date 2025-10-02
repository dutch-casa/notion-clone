namespace Backend.Domain.ValueObjects;

/// <summary>
/// ResourceType value object defining types of resources that can have permissions.
/// Part of Authorization bounded context.
/// </summary>
public readonly struct ResourceType : IEquatable<ResourceType>
{
    public string Value { get; }

    private ResourceType(string value)
    {
        Value = value;
    }

    public static ResourceType Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Invalid resource type", nameof(input));
        }

        var normalized = input.Trim().ToLowerInvariant();
        if (normalized != "org" && normalized != "page" && normalized != "file")
        {
            throw new ArgumentException($"Invalid resource type: {input}", nameof(input));
        }

        return new ResourceType(normalized);
    }

    public static ResourceType Org => new("org");
    public static ResourceType Page => new("page");
    public static ResourceType File => new("file");

    public bool Equals(ResourceType other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is ResourceType type && Equals(type);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(ResourceType left, ResourceType right) => left.Equals(right);
    public static bool operator !=(ResourceType left, ResourceType right) => !(left == right);

    public static implicit operator string(ResourceType type) => type.Value;

    public override string ToString() => Value;
}
