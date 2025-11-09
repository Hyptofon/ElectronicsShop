using Application.Common.Interfaces.Repositories;
using Application.ProductReviews.Exceptions;
using Domain.Products;
using LanguageExt;
using MediatR;

namespace Application.ProductReviews.Commands;

public record ModerateProductReviewCommand(Guid ReviewId)
    : IRequest<Either<ProductReviewException, ProductReview>>;

public class ModerateProductReviewCommandHandler(IProductReviewRepository reviewRepository)
    : IRequestHandler<ModerateProductReviewCommand, Either<ProductReviewException, ProductReview>>
{
    public async Task<Either<ProductReviewException, ProductReview>> Handle(
        ModerateProductReviewCommand request,
        CancellationToken cancellationToken)
    {
        var reviewId = new ProductReviewId(request.ReviewId);
        var existingReview = await reviewRepository.GetByIdAsync(reviewId, cancellationToken);

        return await existingReview.MatchAsync(
            review => ModerateEntity(review, cancellationToken),
            () => Task.FromResult<Either<ProductReviewException, ProductReview>>(
                new ProductReviewNotFoundException(reviewId)));
    }

    private async Task<Either<ProductReviewException, ProductReview>> ModerateEntity(
        ProductReview review,
        CancellationToken cancellationToken)
    {
        try
        {
            review.Moderate();
            return await reviewRepository.UpdateAsync(review, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledProductReviewException(review.Id, exception);
        }
    }
}