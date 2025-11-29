using Api.Dtos;
using Api.Modules.Errors;
using Application.Users.Commands;
using Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UsersController(ISender sender) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<UserDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var query = new GetUserByIdQuery(userGuid);
        var user = await sender.Send(query, cancellationToken);

        return user.Match<ActionResult<UserDto>>(
            u => UserDto.FromDetailsDto(u),
            () => NotFound());
    }

    [HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile(
        [FromBody] UpdateUserProfileDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserProfileCommand
        {
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<UserDto>>(
            user =>
            {
                var roles = User.Claims
                    .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();
                return UserDto.FromDomainModel(user, roles);
            },
            e => e.ToObjectResult());
    }

    [Authorize(Roles = "Admin, Manager")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var query = new GetAllUsersQuery();
        var users = await sender.Send(query, cancellationToken);
        return users.Select(UserDto.FromDetailsDto).ToList();
    }

    [Authorize(Roles = "Admin, Manager")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(id);
        var user = await sender.Send(query, cancellationToken);

        return user.Match<ActionResult<UserDto>>(
            u => UserDto.FromDetailsDto(u),
            () => NotFound());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/block")]
    public async Task<ActionResult<UserDto>> Block(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new BlockUserCommand(id);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<UserDto>>(
            user => UserDto.FromDomainModel(user, new List<string>()),
            e => e.ToObjectResult());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/unblock")]
    public async Task<ActionResult<UserDto>> Unblock(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new UnblockUserCommand(id);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<UserDto>>(
            user => UserDto.FromDomainModel(user, new List<string>()),
            e => e.ToObjectResult());
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<UserDto>> ChangeRole(
        [FromRoute] Guid id,
        [FromBody] ChangeUserRoleDto request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeUserRoleCommand(id, request.RoleName);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<UserDto>>(
            user => UserDto.FromDomainModel(user, new List<string> { request.RoleName }),
            e => e.ToObjectResult());
    }
}