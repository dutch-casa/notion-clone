using Backend.Domain.ValueObjects;

namespace Backend.Domain.Entities;

/// <summary>
/// User entity representing a user account.
/// Part of IdentityOrg bounded context.
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; }
    public string Name { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public User(Email email, string name, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));
        }

        Id = Guid.NewGuid();
        Email = email;
        Name = name.Trim();
        PasswordHash = passwordHash;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty", nameof(name));
        }

        Name = name.Trim();
    }

    // EF Core constructor
    private User() { }
}
