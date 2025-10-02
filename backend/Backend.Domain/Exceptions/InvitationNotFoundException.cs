namespace Backend.Domain.Exceptions;

/// <summary>
/// Exception thrown when an invitation cannot be found.
/// </summary>
public class InvitationNotFoundException : EntityNotFoundException
{
    public InvitationNotFoundException(Guid invitationId)
        : base("Invitation", invitationId)
    {
    }
}
