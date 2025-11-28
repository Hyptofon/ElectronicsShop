using Application.Authentication.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class AuthenticationErrorFactory
{
    public static ObjectResult ToObjectResult(this AuthenticationException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                UserAlreadyExistsException => StatusCodes.Status409Conflict,
                InvalidCredentialsException => StatusCodes.Status401Unauthorized,
                UserNotFoundException => StatusCodes.Status404NotFound,
                UserBlockedException => StatusCodes.Status403Forbidden,
                InvalidRoleException => StatusCodes.Status400BadRequest,
                TokenRefreshFailedException => StatusCodes.Status500InternalServerError,
                UnhandledAuthenticationException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("Authentication error handler not implemented")
            }
        };
    }
}