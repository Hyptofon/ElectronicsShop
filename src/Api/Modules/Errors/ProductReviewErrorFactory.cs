using Application.ProductReviews.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class ProductReviewErrorFactory
{
    public static ObjectResult ToObjectResult(this ProductReviewException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                ProductReviewNotFoundException => StatusCodes.Status404NotFound,
                ProductNotFoundForReviewException => StatusCodes.Status404NotFound,
                UnauthorizedReviewAccessException => StatusCodes.Status403Forbidden,
                ProductReviewAlreadyExistsException => StatusCodes.Status409Conflict,
                UnhandledProductReviewException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("Product review error handler not implemented")
            }
        };
    }
}