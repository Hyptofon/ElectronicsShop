using Application.Orders.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class OrderErrorFactory
{
    public static ObjectResult ToObjectResult(this OrderException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                OrderNotFoundException => StatusCodes.Status404NotFound,
                EmptyCartException => StatusCodes.Status400BadRequest,
                InsufficientStockForOrderException => StatusCodes.Status400BadRequest,
                UnauthorizedOrderAccessException => StatusCodes.Status403Forbidden,
                InvalidOrderStatusTransitionException => StatusCodes.Status400BadRequest,
                UnhandledOrderException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("Order error handler not implemented")
            }
        };
    }
}