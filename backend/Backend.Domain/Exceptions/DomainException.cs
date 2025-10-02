namespace Backend.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-specific exceptions.
/// Domain exceptions represent business rule violations and invalid domain operations.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
