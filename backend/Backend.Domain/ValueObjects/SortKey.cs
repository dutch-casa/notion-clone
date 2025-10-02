namespace Backend.Domain.ValueObjects;

/// <summary>
/// SortKey value object using fractional indexing for efficient reordering.
/// Enables O(1) insertions without renumbering siblings.
/// Uses NUMERIC(18,9) precision to match database schema.
/// </summary>
public readonly struct SortKey : IEquatable<SortKey>, IComparable<SortKey>
{
    public decimal Value { get; }

    private SortKey(decimal value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a SortKey with the specified value.
    /// Value must be positive.
    /// </summary>
    public static SortKey Create(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentException("SortKey value must be positive", nameof(value));
        }

        return new SortKey(value);
    }

    /// <summary>
    /// Generates a new SortKey between two existing keys using fractional indexing.
    /// This enables O(1) insertions without renumbering other items.
    /// </summary>
    /// <param name="before">The key before the insertion point (null if prepending)</param>
    /// <param name="after">The key after the insertion point (null if appending)</param>
    /// <returns>A new SortKey positioned between before and after</returns>
    public static SortKey Between(SortKey? before, SortKey? after)
    {
        // Both null: return default starting position
        if (before == null && after == null)
        {
            return First;
        }

        // Only before exists: append after it
        if (after == null)
        {
            return new SortKey(before!.Value.Value + 1m);
        }

        // Only after exists: prepend before it
        if (before == null)
        {
            return new SortKey(after.Value.Value / 2m);
        }

        // Both exist: find midpoint
        var midpoint = (before.Value.Value + after.Value.Value) / 2m;
        return new SortKey(midpoint);
    }

    /// <summary>
    /// Default first position for a new sequence.
    /// </summary>
    public static SortKey First => new(1m);

    public int CompareTo(SortKey other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString("F9"); // Format with 9 decimal places

    public bool Equals(SortKey other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is SortKey key && Equals(key);

    public override int GetHashCode() => Value.GetHashCode();

    // Comparison operators
    public static bool operator ==(SortKey left, SortKey right) => left.Equals(right);
    public static bool operator !=(SortKey left, SortKey right) => !(left == right);
    public static bool operator <(SortKey left, SortKey right) => left.CompareTo(right) < 0;
    public static bool operator >(SortKey left, SortKey right) => left.CompareTo(right) > 0;
    public static bool operator <=(SortKey left, SortKey right) => left.CompareTo(right) <= 0;
    public static bool operator >=(SortKey left, SortKey right) => left.CompareTo(right) >= 0;

    public static implicit operator decimal(SortKey key) => key.Value;
}
