using System.Net.Mail;

namespace Backend.Domain.ValueObjects;

/// <summary>
/// Email value object ensuring valid email format and normalization.
/// Immutable and equality by value.
/// </summary>
public readonly struct Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates an Email value object from a string.
    /// Validates format, normalizes to lowercase, and trims whitespace.
    /// </summary>
    public static Email Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Email required", nameof(input));
        }

        try
        {
            // Use MailAddress for validation
            var mailAddress = new MailAddress(input);
            var normalized = mailAddress.Address.Trim().ToLowerInvariant();
            return new Email(normalized);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid email format", nameof(input));
        }
    }

    public override string ToString() => Value;

    public bool Equals(Email other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is Email email && Equals(email);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(Email left, Email right) => left.Equals(right);

    public static bool operator !=(Email left, Email right) => !(left == right);

    public static implicit operator string(Email email) => email.Value;
}
