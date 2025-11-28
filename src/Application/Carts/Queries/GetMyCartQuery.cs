using Application.Carts.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Domain.Cart;
using LanguageExt;
using MediatR;

namespace Application.Carts.Queries;

public record GetMyCartQuery : IRequest<Either<CartException, Cart>>;

public class GetMyCartQueryHandler(
    ICartRepository cartRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetMyCartQuery, Either<CartException, Cart>>
{
    public async Task<Either<CartException, Cart>> Handle(
        GetMyCartQuery request, 
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return new UnauthorizedCartAccessException(CartId.Empty());
        }
        
        var cartOption = await cartRepository.GetByUserIdAsync(
            currentUserService.UserId.Value,
            cancellationToken);
        return cartOption.Match<Either<CartException, Cart>>(
            some => some, 
            () => Cart.New(currentUserService.UserId.Value) 
        );
    }
}