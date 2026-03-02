namespace EnterpriseWeb.Application.Interfaces;

using EnterpriseWeb.Application.DTOs.User;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<Guid> CreateUserAsync(CreateUserDto dto, Guid? createdBy = null);
    Task UpdateUserAsync(UpdateUserDto dto, Guid? updatedBy = null);
    Task DeleteUserAsync(Guid id);
}
