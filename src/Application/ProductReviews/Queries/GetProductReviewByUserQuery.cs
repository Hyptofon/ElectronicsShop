using Application.Common.Interfaces.Repositories;
using Domain.Products;
using LanguageExt;
using MediatR;

namespace Application.ProductReviews.Queries;

public record GetProductReviewByUserQuery(Guid ProductId, Guid UserId) : IRequest<Option<ProductReview>>;

public class GetProductReviewByUserQueryHandler(IProductReviewRepository reviewRepository)
    : IRequestHandler<GetProductReviewByUserQuery, Option<ProductReview>>
{
    public async Task<Option<ProductReview>> Handle(
        GetProductReviewByUserQuery request,
        CancellationToken cancellationToken)
    {
        return await reviewRepository.GetByProductAndUserAsync(
            new ProductId(request.ProductId),
            request.UserId,
            cancellationToken);
    }
}