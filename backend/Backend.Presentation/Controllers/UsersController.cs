using Backend.Application.UseCases.Users.UpdateUserProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UpdateUserProfileHandler _updateUserProfileHandler;

    public UsersController(UpdateUserProfileHandler updateUserProfileHandler)
    {
        _updateUserProfileHandler = updateUserProfileHandler;
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UpdateUserProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateUserProfileResponseDto>> UpdateProfile(
        Guid id,
        [FromBody] UpdateUserProfileRequestDto request)
    {
        var command = new UpdateUserProfileCommand(id, request.Name);
        var result = await _updateUserProfileHandler.HandleAsync(command);

        var response = new UpdateUserProfileResponseDto
        {
            Id = result.Id,
            Email = result.Email,
            Name = result.Name
        };

        return Ok(response);
    }
}

// DTOs
public record UpdateUserProfileRequestDto
{
    public required string Name { get; init; }
}

public record UpdateUserProfileResponseDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
}
