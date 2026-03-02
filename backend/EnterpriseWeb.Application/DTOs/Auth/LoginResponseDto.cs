namespace EnterpriseWeb.Application.DTOs.Auth;

public record LoginResponseDto(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    UserInfoDto User
);

public record UserInfoDto(
    Guid Id,
    string Username,
    string Email,
    IEnumerable<string> Roles,
    IEnumerable<string> Permissions
);
