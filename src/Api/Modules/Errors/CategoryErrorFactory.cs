using Application.Categories.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class CategoryErrorFactory
{
    public static ObjectResult ToObjectResult(this CategoryException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                CategoryAlreadyExistException => StatusCodes.Status409Conflict,
                CategoryNotFoundException => StatusCodes.Status404NotFound,
                CategoryHasProductsException => StatusCodes.Status400BadRequest,
                UnhandledCategoryException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("Category error handler not implemented")
            }
        };
    }
}