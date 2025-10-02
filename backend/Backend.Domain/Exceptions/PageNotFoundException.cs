namespace Backend.Domain.Exceptions;

/// <summary>
/// Exception thrown when a page cannot be found.
/// </summary>
public class PageNotFoundException : EntityNotFoundException
{
    public PageNotFoundException(Guid pageId)
        : base("Page", pageId)
    {
    }
}
