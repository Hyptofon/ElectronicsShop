using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.ProductReviews.Exceptions;
using Domain.Products;
using LanguageExt;
using MediatR;

namespace Application.ProductReviews.Commands;

public record CreateProductReviewCommand(Guid ProductId, int Rating, string Comment)
    : IRequest<Either<ProductReviewException, ProductReview>>;

public class CreateProductReviewCommandHandler(
    IProductReviewRepository reviewRepository,
    IProductRepository productRepository,
    ICurrentUserService currentUserService,
    IApplicationDbContext dbContext)
    : IRequestHandler<CreateProductReviewCommand, Either<ProductReviewException, ProductReview>>
{
    public async Task<Either<ProductReviewException, ProductReview>> Handle(
        CreateProductReviewCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return new UnauthorizedReviewAccessException(ProductReviewId.Empty());
        }

        var productId = new ProductId(request.ProductId);
        var userId = currentUserService.UserId.Value;

        var existingReviewOption = await reviewRepository.GetByProductAndUserAsync(
            productId,
            userId,
            cancellationToken);

        var product = await productRepository.GetByIdAsync(productId, cancellationToken);
        
        return await product.MatchAsync(
            p => existingReviewOption.Match(
                review => UpdateExistingReview(review, request, cancellationToken),
                () => CreateEntity(request, p.Id, userId, cancellationToken)
            ),
            () => Task.FromResult<Either<ProductReviewException, ProductReview>>(
                new ProductNotFoundForReviewException(productId)));
    }

    private async Task<Either<ProductReviewException, ProductReview>> CreateEntity(
        CreateProductReviewCommand request,
        ProductId productId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var review = ProductReview.New(productId, userId, request.Rating, request.Comment);
            reviewRepository.Add(review);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            return review;
        }
        catch (Exception exception)
        {
            return new UnhandledProductReviewException(ProductReviewId.Empty(), exception);
        }
    }
    
    private async Task<Either<ProductReviewException, ProductReview>> UpdateExistingReview(
        ProductReview review,
        CreateProductReviewCommand request,
        CancellationToken cancellationToken)
    {
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