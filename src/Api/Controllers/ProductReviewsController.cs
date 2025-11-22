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
    
    
    [Authorize]
    [HttpGet("my")]
    public async Task<ActionResult<IReadOnlyList<ProductReviewDto>>> GetMyReview(
        [FromRoute] Guid productId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var query = new GetProductReviewByUserQuery(productId, userGuid);
        var reviewOption = await sender.Send(query, cancellationToken);
        
        return reviewOption.Match<ActionResult<IReadOnlyList<ProductReviewDto>>>(
            r => new List<ProductReviewDto> { ProductReviewDto.FromDomainModel(r) },
            () => new List<ProductReviewDto>());
    }
}