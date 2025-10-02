namespace Backend.Domain.Exceptions;

/// <summary>
/// Exception thrown when a user cannot be found.
/// </summary>
public class UserNotFoundException : EntityNotFoundException
{
    public UserNotFoundException(Guid userId)
        : base("User", userId)
    {
    }
}
