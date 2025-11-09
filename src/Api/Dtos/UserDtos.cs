using Application.Users.Queries;
using Domain.Users;

namespace Api.Dtos;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsBlocked,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles)
{
    public static UserDto FromDomainModel(ApplicationUser user, IReadOnlyList<string> roles)
        => new(user.Id, user.Email!, user.FirstName, user.LastName, user.IsBlocked, user.CreatedAt, roles);

    public static UserDto FromDetailsDto(UserDetailsDto dto)
        => new(dto.Id, dto.Email, dto.FirstName, dto.LastName, dto.IsBlocked, dto.CreatedAt, dto.Roles);
}

public record UpdateUserProfileDto(string FirstName, string LastName);

public record ChangeUserRoleDto(string RoleName);