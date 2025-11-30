using Application.Users.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Application.Common.Interfaces.Repositories;

namespace Application.Users.Commands;

public record DeleteUserCommand(Guid UserId) 
    : IRequest<Either<UserException, ApplicationUser>>;

public class DeleteUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    ICartRepository cartRepository,
    IOrderRepository orderRepository,
    IProductReviewRepository reviewRepository)
    : IRequestHandler<DeleteUserCommand, Either<UserException, ApplicationUser>>
{
    public async Task<Either<UserException, ApplicationUser>> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());

        if (user == null)
        {
            return new UserNotFoundException(request.UserId.ToString());
        }
        
        var hasCarts = await cartRepository.ExistsByUserIdAsync(user.Id, cancellationToken);
        if (hasCarts)
        {
            return new UserCannotBeDeletedDueToCartException(user.Id);
        }
        
        var hasOrders = await orderRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (hasOrders.Any())
        {
            return new UserCannotBeDeletedDueToOrdersException(user.Id);
        }
        
        var hasReviews = await reviewRepository.ExistsByUserIdAsync(user.Id, cancellationToken);
        if (hasReviews)
        {
            return new UserCannotBeDeletedDueToReviewsException(user.Id);
        }

        try
        {
            var result = await userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new UserDeleteFailedException(errors);
            }

            return user;
        }
        catch (Exception exception)
        {
            return new UnhandledUserException(exception);
        }
    }
}