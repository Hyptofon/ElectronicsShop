using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Domain.Cart;
using LanguageExt;
using MediatR;

namespace Application.Carts.Queries;

public record GetMyCartQuery : IRequest<Option<Cart>>;

public class GetMyCartQueryHandler(
    ICartRepository cartRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetMyCartQuery, Option<Cart>>
{
    public async Task<Option<Cart>> Handle(GetMyCartQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return Option<Cart>.None;
        }

        return await cartRepository.GetByUserIdAsync(
            currentUserService.UserId.Value,
            cancellationToken);
    }
}