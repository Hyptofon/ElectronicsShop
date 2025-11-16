using Api.Dtos;
using Api.Modules.Errors;
using Application.Authentication.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthenticationController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthenticationResponseDto>> Register(
        [FromBody] RegisterDto request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand
        {
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<AuthenticationResponseDto>>(
            r => AuthenticationResponseDto.FromResult(r),
            e => e.ToObjectResult());
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResponseDto>> Login(
        [FromBody] LoginDto request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand
        {
            Email = request.Email,
            Password = request.Password
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<AuthenticationResponseDto>>(
            r => AuthenticationResponseDto.FromResult(r),
            e => e.ToObjectResult());
    }
    
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthenticationResponseDto>> RefreshToken(
        [FromBody] RefreshTokenDto request, 
        CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand
        {
            Token = request.Token,
            RefreshToken = request.RefreshToken
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<AuthenticationResponseDto>>(
            r => AuthenticationResponseDto.FromResult(r),
            e => e.ToObjectResult());
    }
}