namespace Backend.Application.UseCases.Users.UpdateUserProfile;

public record UpdateUserProfileCommand(Guid UserId, string Name);
