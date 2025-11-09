using Domain.Users;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Users.Queries;

public record GetUserByIdQuery(Guid UserId) : IRequest<Option<UserDetailsDto>>;

public class GetUserByIdQueryHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<GetUserByIdQuery, Option<UserDetailsDto>>
{
    public async Task<Option<UserDetailsDto>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());

        if (user == null)
        {
            return Option<UserDetailsDto>.None;
        }

        var roles = await userManager.GetRolesAsync(user);

        return new UserDetailsDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsBlocked = user.IsBlocked,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };
    }
}