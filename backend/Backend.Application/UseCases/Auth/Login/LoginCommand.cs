namespace Backend.Application.UseCases.Auth.Login;

public record LoginCommand(string Email, string Password);

public record LoginResult(Guid UserId, string Email, string Name, string Token);
