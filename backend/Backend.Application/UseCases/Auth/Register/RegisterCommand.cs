namespace Backend.Application.UseCases.Auth.Register;

public record RegisterCommand(string Email, string Password, string Name);

public record RegisterResult(Guid UserId, string Email, string Name, string Token);
