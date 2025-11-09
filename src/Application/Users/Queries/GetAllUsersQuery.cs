using Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Users.Queries;

public record GetAllUsersQuery : IRequest<IReadOnlyList<UserDetailsDto>>;

public class GetAllUsersQueryHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<GetAllUsersQuery, IReadOnlyList<UserDetailsDto>>
{
    public async Task<IReadOnlyList<UserDetailsDto>> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await userManager.Users.ToListAsync(cancellationToken);
        var userDetailsList = new List<UserDetailsDto>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            userDetailsList.Add(new UserDetailsDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsBlocked = user.IsBlocked,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList()
            });
        }

        return userDetailsList;
    }
}

public record UserDetailsDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required bool IsBlocked { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
}