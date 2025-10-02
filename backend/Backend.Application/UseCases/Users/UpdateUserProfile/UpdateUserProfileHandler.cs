using Backend.Domain.Repositories;

namespace Backend.Application.UseCases.Users.UpdateUserProfile;

public class UpdateUserProfileHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserProfileHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateUserProfileResult> HandleAsync(
        UpdateUserProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        // Find the user
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException($"User {command.UserId} not found");
        }

        // Update the user's name
        user.UpdateName(command.Name);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateUserProfileResult(
            user.Id,
            user.Email.Value,
            user.Name);
    }
}
