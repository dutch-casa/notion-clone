using Backend.Domain.Repositories;
using Backend.Domain.ValueObjects;

namespace Backend.Application.UseCases.Organizations.UpdateMemberRole;

public class UpdateMemberRoleHandler
{
    private readonly IOrgRepository _orgRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMemberRoleHandler(
        IOrgRepository orgRepository,
        IUnitOfWork unitOfWork)
    {
        _orgRepository = orgRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateMemberRoleResult> HandleAsync(
        UpdateMemberRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get organization with members
        var org = await _orgRepository.GetByIdWithMembersAsync(command.OrgId, cancellationToken)
            ?? throw new InvalidOperationException("Organization not found");

        // Authorization check - only owners and admins can update roles
        var requestingMember = org.Members.FirstOrDefault(m => m.UserId == command.RequestingUserId)
            ?? throw new UnauthorizedAccessException("Only owners and admins can update member roles");

        if (!requestingMember.Role.CanInviteMembers)
        {
            throw new UnauthorizedAccessException("Only owners and admins can update member roles");
        }

        // Find target member
        var targetMember = org.Members.FirstOrDefault(m => m.Id == command.MemberId)
            ?? throw new InvalidOperationException("Member not found");

        // Parse and validate new role
        var newRole = OrgRole.Create(command.NewRole);

        // Update the role
        targetMember.UpdateRole(newRole);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateMemberRoleResult(
            targetMember.Id,
            targetMember.UserId,
            targetMember.Role.Value,
            targetMember.JoinedAt
        );
    }
}
