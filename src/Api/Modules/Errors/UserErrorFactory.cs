using Application.Users.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class UserErrorFactory
{
    public static ObjectResult ToObjectResult(this UserException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                UserNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedUserAccessException => StatusCodes.Status403Forbidden,
                UserBlockedException => StatusCodes.Status403Forbidden,
                InvalidRoleException => StatusCodes.Status400BadRequest,
                UserCannotBeDeletedDueToCartException => StatusCodes.Status409Conflict,
                UserCannotBeDeletedDueToOrdersException => StatusCodes.Status409Conflict,
                UserCannotBeDeletedDueToReviewsException => StatusCodes.Status409Conflict,
                UserDeleteFailedException => StatusCodes.Status500InternalServerError,
                UnhandledUserException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("User error handler not implemented")
            }
        };
    }
}