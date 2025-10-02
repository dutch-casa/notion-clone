using Backend.Application.Services;
using Backend.Domain.Entities;
using Backend.Domain.Repositories;
using Backend.Domain.ValueObjects;

namespace Backend.Application.UseCases.Auth.Register;

public class RegisterHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<RegisterResult> HandleAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate email format
        var email = Email.Create(command.Email);

        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email {command.Email} already exists");
        }

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(command.Password);

        // Create user
        var user = new User(email, command.Name, passwordHash);

        // Save to database
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate JWT token
        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email.Value, user.Name);

        return new RegisterResult(user.Id, user.Email.Value, user.Name, token);
    }
}
