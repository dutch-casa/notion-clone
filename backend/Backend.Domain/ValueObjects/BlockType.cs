namespace Backend.Domain.ValueObjects;

/// <summary>
/// Block type value object representing supported block types in documents.
/// Extensible design: add new block types by adding to the AllowedTypes set.
/// </summary>
public readonly struct BlockType : IEquatable<BlockType>
{
    // Centralized list of allowed block types - add new types here
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "paragraph",
        "heading",
        "todo",
        "file",
        "image",
        // Add new block types here as needed:
        // "code",
        // "video",
        // "table",
        // etc.
    };

    public string Value { get; }

    private BlockType(string value)
    {
        Value = value;
    }

    public static BlockType Create(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Block type cannot be empty", nameof(type));
        }

        var normalized = type.Trim().ToLowerInvariant();

        if (!AllowedTypes.Contains(normalized))
        {
            throw new ArgumentException(
                $"Invalid block type: {type}. Allowed types: {string.Join(", ", AllowedTypes.OrderBy(x => x))}",
                nameof(type));
        }

        return new BlockType(normalized);
    }

    // Pre-defined types for convenience
    public static BlockType Paragraph => new("paragraph");
    public static BlockType Heading => new("heading");
    public static BlockType Todo => new("todo");
    public static BlockType File => new("file");
    public static BlockType Image => new("image");

    // Type checks
    public bool IsParagraph => Value == "paragraph";
    public bool IsHeading => Value == "heading";
    public bool IsTodo => Value == "todo";
    public bool IsFile => Value == "file";
    public bool IsImage => Value == "image";

    public override string ToString() => Value;

    public bool Equals(BlockType other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is BlockType type && Equals(type);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(BlockType left, BlockType right) => left.Equals(right);

    public static bool operator !=(BlockType left, BlockType right) => !(left == right);

    public static implicit operator string(BlockType type) => type.Value;
}
