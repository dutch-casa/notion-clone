namespace Backend.Domain.Exceptions;

/// <summary>
/// Exception thrown when a user attempts an unauthorized operation on an organization.
/// </summary>
public class UnauthorizedOrgAccessException : DomainException
{
    public Guid UserId { get; }
    public Guid OrganizationId { get; }
    public string? Operation { get; }

    public UnauthorizedOrgAccessException(Guid userId, Guid organizationId, string? operation = null)
        : base(BuildMessage(userId, organizationId, operation))
    {
        UserId = userId;
        OrganizationId = organizationId;
        Operation = operation;
    }

    private static string BuildMessage(Guid userId, Guid organizationId, string? operation)
    {
        var baseMessage = $"User '{userId}' is not authorized to access organization '{organizationId}'";
        return operation != null ? $"{baseMessage} for operation: {operation}" : baseMessage;
    }
}
