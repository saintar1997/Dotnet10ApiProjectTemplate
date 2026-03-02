namespace EnterpriseWeb.Application.Interfaces;

using EnterpriseWeb.Application.DTOs.Auth;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
}
