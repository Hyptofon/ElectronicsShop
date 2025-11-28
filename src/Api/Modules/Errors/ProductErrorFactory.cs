using Application.Products.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class ProductErrorFactory
{
    public static ObjectResult ToObjectResult(this ProductException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                ProductAlreadyExistException => StatusCodes.Status409Conflict,
                ProductNotFoundException => StatusCodes.Status404NotFound,
                ProductCategoriesNotFoundException => StatusCodes.Status404NotFound,
                ProductImageNotFoundException => StatusCodes.Status404NotFound, 
                InsufficientStockException => StatusCodes.Status400BadRequest,
                UnhandledProductException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("Product error handler not implemented")
            }
        };
    }
}