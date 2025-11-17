using Application.Common.Interfaces.Repositories;
using Domain.Products;
using MediatR;

namespace Application.ProductReviews.Queries;

public record GetUnmoderatedReviewsQuery : IRequest<IReadOnlyList<ProductReview>>;

public class GetUnmoderatedReviewsQueryHandler(IProductReviewRepository reviewRepository)
    : IRequestHandler<GetUnmoderatedReviewsQuery, IReadOnlyList<ProductReview>>
{
    public async Task<IReadOnlyList<ProductReview>> Handle(
        GetUnmoderatedReviewsQuery request,
        CancellationToken cancellationToken)
    {
        return await reviewRepository.GetAllUnmoderatedAsync(cancellationToken);
    }
}