namespace EnterpriseWeb.Application.DTOs.User;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    bool IsActive,
    DateTime CreatedAt,
    IEnumerable<string> Roles
);

public record CreateUserDto(
    string Username,
    string Email,
    string Password,
    IEnumerable<Guid>? RoleIds
);

public record UpdateUserDto(
    Guid Id,
    string Username,
    string Email,
    bool IsActive,
    IEnumerable<Guid>? RoleIds
);
