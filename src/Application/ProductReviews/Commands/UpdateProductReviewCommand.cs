using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.ProductReviews.Exceptions;
using Domain.Products;
using LanguageExt;
using MediatR;

namespace Application.ProductReviews.Commands;

public record UpdateProductReviewCommand(Guid ReviewId, int Rating, string Comment)
    : IRequest<Either<ProductReviewException, ProductReview>>;

public class UpdateProductReviewCommandHandler(
    IProductReviewRepository reviewRepository,
    ICurrentUserService currentUserService,
    IApplicationDbContext dbContext)
    : IRequestHandler<UpdateProductReviewCommand, Either<ProductReviewException, ProductReview>>
{
    public async Task<Either<ProductReviewException, ProductReview>> Handle(
        UpdateProductReviewCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return new UnauthorizedReviewAccessException(ProductReviewId.Empty());
        }

        var reviewId = new ProductReviewId(request.ReviewId);
        var existingReview = await reviewRepository.GetByIdAsync(reviewId, cancellationToken);

        return await existingReview.MatchAsync(
            review => UpdateEntity(review, request, currentUserService.UserId.Value, cancellationToken),
            () => Task.FromResult<Either<ProductReviewException, ProductReview>>(
                new ProductReviewNotFoundException(reviewId)));
    }

    private async Task<Either<ProductReviewException, ProductReview>> UpdateEntity(
        ProductReview review,
        UpdateProductReviewCommand request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (review.UserId != userId && !currentUserService.IsInRole("Admin") && !currentUserService.IsInRole("Manager"))
        {
            return new UnauthorizedReviewAccessException(review.Id);
        }

        try
        {
            review.UpdateReview(request.Rating, request.Comment);
            reviewRepository.Update(review);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            return review;
        }
        catch (Exception exception)
        {
            return new UnhandledProductReviewException(review.Id, exception);
        }
    }
}