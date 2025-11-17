using Api.Dtos;
using Api.Modules.Errors;
using Application.ProductReviews.Commands;
using Application.ProductReviews.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("reviews")]
public class ReviewsController(ISender sender) : ControllerBase
{
    [Authorize(Roles = "User,Manager,Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductReviewDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateProductReviewDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductReviewCommand(id, request.Rating, request.Comment);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<ProductReviewDto>>(
            r => ProductReviewDto.FromDomainModel(r),
            e => e.ToObjectResult());
    }

    [Authorize(Roles = "User,Manager,Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ProductReviewDto>> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteProductReviewCommand(id);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<ProductReviewDto>>(
            r => ProductReviewDto.FromDomainModel(r),
            e => e.ToObjectResult());
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("unmoderated")]
    public async Task<ActionResult<IReadOnlyList<ProductReviewDto>>> GetUnmoderated(
        CancellationToken cancellationToken)
    {
        var query = new GetUnmoderatedReviewsQuery(); 
        var reviews = await sender.Send(query, cancellationToken);
        return reviews.Select(ProductReviewDto.FromDomainModel).ToList();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/moderate")]
    public async Task<ActionResult<ProductReviewDto>> Moderate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new ModerateProductReviewCommand(id);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<ProductReviewDto>>(
            r => ProductReviewDto.FromDomainModel(r),
            e => e.ToObjectResult());
    }
}