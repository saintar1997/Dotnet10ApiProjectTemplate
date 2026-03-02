namespace EnterpriseWeb.Application.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EnterpriseWeb.Application.DTOs.Auth;
using EnterpriseWeb.Application.Interfaces;
using EnterpriseWeb.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public class AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, IConfiguration configuration) : IAuthService
{
    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await userRepository.GetByUsernameAsync(request.Username);
        if (user is null || !user.IsActive)
            return null;

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return null;

        var permissionCodes = await userRepository.GetPermissionCodesAsync(user.Id);
        var roleNames = user.Roles.Select(r => r.Name).ToList();

        var token = GenerateJwtToken(user.Id, user.Username, user.Email, roleNames, permissionCodes);
        var expiresIn = int.Parse(configuration["Jwt:ExpiresInMinutes"] ?? "60") * 60;

        return new LoginResponseDto(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: expiresIn,
            User: new UserInfoDto(user.Id, user.Username, user.Email, roleNames, permissionCodes)
        );
    }

    private string GenerateJwtToken(
        Guid userId,
        string username,
        string email,
        IEnumerable<string> roles,
        IEnumerable<string> permissions)
    {
        var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var expiresInMinutes = int.Parse(configuration["Jwt:ExpiresInMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
