using Application.Carts.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class CartErrorFactory
{
    public static ObjectResult ToObjectResult(this CartException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                CartNotFoundException => StatusCodes.Status404NotFound,
                CartItemNotFoundException => StatusCodes.Status404NotFound,
                ProductNotFoundForCartException => StatusCodes.Status404NotFound,
                InsufficientStockForCartException => StatusCodes.Status400BadRequest,
                UnauthorizedCartAccessException => StatusCodes.Status403Forbidden,
                UnhandledCartException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("Cart error handler not implemented")
            }
        };
    }
}