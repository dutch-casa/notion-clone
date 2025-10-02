namespace Backend.Domain.Exceptions;

/// <summary>
/// Exception thrown when a requested entity cannot be found.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityType { get; }
    public object EntityId { get; }

    public EntityNotFoundException(string entityType, object entityId)
        : base($"{entityType} with ID '{entityId}' was not found.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}
