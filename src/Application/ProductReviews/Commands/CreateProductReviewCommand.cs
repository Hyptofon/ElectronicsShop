// D:\Rider прожекти\Practicals\ElectronicsShop\src\Application\ProductReviews\Commands\CreateProductReviewCommand.cs

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
    ICurrentUserService currentUserService)
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

        // 1. ПЕРЕВІРКА НА ДУБЛІКАТ: Користувач може залишити лише один відгук на продукт.
        var existingReview = await reviewRepository.GetByProductAndUserAsync(
            productId,
            userId,
            cancellationToken);

        if (existingReview.IsSome)
        {
            // Повертаємо виняток, який повинен бути перехоплений API-шаром і перетворений на 409 Conflict.
            return new ProductReviewAlreadyExistsException(productId, userId);
        }
        
        // 2. ПЕРЕВІРКА НА ІСНУВАННЯ ПРОДУКТУ
        var product = await productRepository.GetByIdAsync(productId, cancellationToken);

        return await product.MatchAsync(
            p => CreateEntity(request, p.Id, userId, cancellationToken),
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
            return await reviewRepository.AddAsync(review, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledProductReviewException(ProductReviewId.Empty(), exception);
        }
    }
}