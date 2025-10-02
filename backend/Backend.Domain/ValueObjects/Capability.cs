namespace Backend.Domain.ValueObjects;

/// <summary>
/// Capability value object representing permission levels with hierarchy.
/// Hierarchy: Admin > Edit > Comment > View
/// Higher capabilities imply all lower capabilities.
/// </summary>
public readonly struct Capability : IEquatable<Capability>
{
    private const string ViewValue = "view";
    private const string CommentValue = "comment";
    private const string EditValue = "edit";
    private const string AdminValue = "admin";

    public string Value { get; }
    public int Level { get; }

    private Capability(string value, int level)
    {
        Value = value;
        Level = level;
    }

    public static Capability Create(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
        {
            throw new ArgumentException("Capability cannot be empty", nameof(capability));
        }

        var normalized = capability.Trim().ToLowerInvariant();

        return normalized switch
        {
            ViewValue => View,
            CommentValue => Comment,
            EditValue => Edit,
            AdminValue => Admin,
            _ => throw new ArgumentException($"Invalid capability: {capability}. Must be view, comment, edit, or admin.", nameof(capability))
        };
    }

    // Pre-defined capabilities with hierarchy levels
    public static Capability View => new(ViewValue, 1);
    public static Capability Comment => new(CommentValue, 2);
    public static Capability Edit => new(EditValue, 3);
    public static Capability Admin => new(AdminValue, 4);

    /// <summary>
    /// Checks if this capability implies (includes) another capability.
    /// Higher capabilities imply all lower capabilities.
    /// </summary>
    public bool Implies(Capability other) => Level >= other.Level;

    public override string ToString() => Value;

    public bool Equals(Capability other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is Capability capability && Equals(capability);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(Capability left, Capability right) => left.Equals(right);

    public static bool operator !=(Capability left, Capability right) => !(left == right);

    public static implicit operator string(Capability capability) => capability.Value;
}
