using Backend.Domain.Aggregates;
using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Organizations.CreateOrganization;

public class CreateOrganizationHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IOrgRepository _orgRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrganizationHandler(
        IUserRepository userRepository,
        IOrgRepository orgRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _orgRepository = orgRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateOrganizationResult> HandleAsync(
        CreateOrganizationCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate that owner exists
        var userExists = await _userRepository.ExistsAsync(command.OwnerId, cancellationToken);
        if (!userExists)
        {
            throw new InvalidOperationException($"User {command.OwnerId} does not exist");
        }

        // Create organization (owner automatically added as member in Org constructor)
        var org = new Org(command.Name, command.OwnerId);

        // Save to database
        await _orgRepository.AddAsync(org, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateOrganizationResult(
            org.Id,
            org.Name,
            org.OwnerId,
            org.CreatedAt);
    }
}
