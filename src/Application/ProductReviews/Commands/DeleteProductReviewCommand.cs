using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.ProductReviews.Exceptions;
using Domain.Products;
using LanguageExt;
using MediatR;

namespace Application.ProductReviews.Commands;

public record DeleteProductReviewCommand(Guid ReviewId)
    : IRequest<Either<ProductReviewException, ProductReview>>;

public class DeleteProductReviewCommandHandler(
    IProductReviewRepository reviewRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteProductReviewCommand, Either<ProductReviewException, ProductReview>>
{
    public async Task<Either<ProductReviewException, ProductReview>> Handle(
        DeleteProductReviewCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return new UnauthorizedReviewAccessException(ProductReviewId.Empty());
        }

        var reviewId = new ProductReviewId(request.ReviewId);
        var existingReview = await reviewRepository.GetByIdAsync(reviewId, cancellationToken);

        return await existingReview.MatchAsync(
            review => DeleteEntity(review, currentUserService.UserId.Value, cancellationToken),
            () => Task.FromResult<Either<ProductReviewException, ProductReview>>(
                new ProductReviewNotFoundException(reviewId)));
    }

    private async Task<Either<ProductReviewException, ProductReview>> DeleteEntity(
        ProductReview review,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (review.UserId != userId && !currentUserService.IsInRole("Admin"))
        {
            return new UnauthorizedReviewAccessException(review.Id);
        }

        try
        {
            return await reviewRepository.DeleteAsync(review, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledProductReviewException(review.Id, exception);
        }
    }
}