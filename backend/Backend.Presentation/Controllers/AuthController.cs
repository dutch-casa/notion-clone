using Backend.Application.UseCases.Auth.Login;
using Backend.Application.UseCases.Auth.Register;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly RegisterHandler _registerHandler;
    private readonly LoginHandler _loginHandler;

    public AuthController(RegisterHandler registerHandler, LoginHandler loginHandler)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
    {
        var command = new RegisterCommand(request.Email, request.Password, request.Name);
        var result = await _registerHandler.HandleAsync(command);

        // Set JWT token in HttpOnly cookie for security (prevents XSS attacks)
        SetAuthCookie(result.Token);

        var response = new AuthResponseDto
        {
            Token = result.Token, // Still return for compatibility, but frontend should rely on cookie
            User = new UserDto
            {
                Id = result.UserId,
                Email = result.Email,
                Name = result.Name
            }
        };

        return CreatedAtAction(nameof(Register), response);
    }

    /// <summary>
    /// Authenticate a user
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _loginHandler.HandleAsync(command);

        // Set JWT token in HttpOnly cookie for security (prevents XSS attacks)
        SetAuthCookie(result.Token);

        var response = new AuthResponseDto
        {
            Token = result.Token, // Still return for compatibility, but frontend should rely on cookie
            User = new UserDto
            {
                Id = result.UserId,
                Email = result.Email,
                Name = result.Name
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Logout - Clear auth cookie
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("auth_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });
        return Ok(new { message = "Logged out successfully" });
    }

    private void SetAuthCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // Prevents JavaScript access (XSS protection)
            Secure = true,   // Required for SameSite=None and HTTPS
            SameSite = SameSiteMode.None, // Required for cross-origin cookies (different subdomains)
            Expires = DateTimeOffset.UtcNow.AddDays(7) // Match JWT expiration
        };

        Response.Cookies.Append("auth_token", token, cookieOptions);
    }
}

// DTOs matching OpenAPI spec
public record RegisterRequestDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string Name { get; init; }
}

public record LoginRequestDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public record AuthResponseDto
{
    public required string Token { get; init; }
    public required UserDto User { get; init; }
}

public record UserDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
}
