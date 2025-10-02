using Backend.Application.Services;
using Backend.Domain.Repositories;
using Backend.Domain.ValueObjects;

namespace Backend.Application.UseCases.Auth.Login;

public class LoginHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResult> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate email format
        var email = Email.Create(command.Email);

        // Find user
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Generate JWT token
        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email.Value, user.Name);

        return new LoginResult(user.Id, user.Email.Value, user.Name, token);
    }
}
