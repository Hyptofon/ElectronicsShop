using Application.Common.Interfaces.Repositories;
using Domain.Products;
using MediatR;

namespace Application.ProductReviews.Queries;

public record GetProductReviewsQuery(Guid ProductId) : IRequest<IReadOnlyList<ProductReview>>;

public class GetProductReviewsQueryHandler(IProductReviewRepository reviewRepository)
    : IRequestHandler<GetProductReviewsQuery, IReadOnlyList<ProductReview>>
{
    public async Task<IReadOnlyList<ProductReview>> Handle(
        GetProductReviewsQuery request,
        CancellationToken cancellationToken)
    {
        return await reviewRepository.GetByProductIdAsync(
            new ProductId(request.ProductId),
            cancellationToken);
    }
}