using Application.Common.Interfaces.Repositories;
using Domain.Products;
using MediatR;

namespace Application.ProductReviews.Queries;

public record GetModeratedReviewsQuery : IRequest<IReadOnlyList<ProductReview>>;

public class GetModeratedReviewsQueryHandler(IProductReviewRepository reviewRepository)
    : IRequestHandler<GetModeratedReviewsQuery, IReadOnlyList<ProductReview>>
{
    public async Task<IReadOnlyList<ProductReview>> Handle(
        GetModeratedReviewsQuery request,
        CancellationToken cancellationToken)
    {
        return await reviewRepository.GetAllModeratedAsync(cancellationToken);
    }
}