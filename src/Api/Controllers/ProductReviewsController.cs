using Api.Dtos;
using Api.Modules.Errors;
using Application.ProductReviews.Commands;
using Application.ProductReviews.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("products/{productId:guid}/reviews")]
public class ProductReviewsController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductReviewDto>>> GetReviews(
        [FromRoute] Guid productId,
        CancellationToken cancellationToken)
    {
        var query = new GetProductReviewsQuery(productId);
        var reviews = await sender.Send(query, cancellationToken);
        return reviews.Select(ProductReviewDto.FromDomainModel).ToList();
    }

    [Authorize(Roles = "User,Manager,Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductReviewDto>> Create(
        [FromRoute] Guid productId,
        [FromBody] CreateProductReviewDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductReviewCommand(productId, request.Rating, request.Comment);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<ProductReviewDto>>(
            r => ProductReviewDto.FromDomainModel(r),
            e => e.ToObjectResult());
    }
}