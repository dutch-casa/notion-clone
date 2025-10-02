using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Organizations.ListOrganizations;

public class ListOrganizationsHandler
{
    private readonly IOrgRepository _orgRepository;

    public ListOrganizationsHandler(IOrgRepository orgRepository)
    {
        _orgRepository = orgRepository;
    }

    public async Task<IEnumerable<OrganizationDto>> HandleAsync(
        ListOrganizationsCommand command,
        CancellationToken cancellationToken = default)
    {
        var organizations = await _orgRepository.GetOrganizationsByUserIdAsync(command.UserId, cancellationToken);

        return organizations.Select(o => new OrganizationDto(
            o.Id,
            o.Name,
            o.OwnerId,
            o.CreatedAt,
            o.Members.First(m => m.UserId == command.UserId).Role.Value
        )).ToList();
    }
}
