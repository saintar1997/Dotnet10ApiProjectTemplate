namespace EnterpriseWeb.Application.Services;

using EnterpriseWeb.Application.DTOs.User;
using EnterpriseWeb.Application.Interfaces;
using EnterpriseWeb.Domain.Entities;
using EnterpriseWeb.Domain.Interfaces;

public class UserService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IUserService
{
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await userRepository.GetAllAsync();
        return users.Select(MapToDto);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);
        return user is null ? null : MapToDto(user);
    }

    public async Task<Guid> CreateUserAsync(CreateUserDto dto, Guid? createdBy = null)
    {
        var passwordHash = passwordHasher.Hash(dto.Password ?? string.Empty);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        unitOfWork.Begin();
        try
        {
            var newId = await userRepository.CreateAsync(
                user, passwordHash, unitOfWork.Connection, unitOfWork.Transaction!);

            if (dto.RoleIds is not null && dto.RoleIds.Any())
            {
                await userRepository.UpdateUserRolesAsync(
                    newId, dto.RoleIds, unitOfWork.Connection, unitOfWork.Transaction!);
            }

            unitOfWork.Commit();
            return newId;
        }
        catch
        {
            unitOfWork.Rollback();
            throw;
        }
    }

    public async Task UpdateUserAsync(UpdateUserDto dto, Guid? updatedBy = null)
    {
        var existingUser = await userRepository.GetByIdAsync(dto.Id);
        if (existingUser is null) return;

        var updatedUser = existingUser with
        {
            Username = dto.Username,
            Email = dto.Email,
            IsActive = dto.IsActive,
            UpdatedBy = updatedBy,
            UpdatedAt = DateTime.UtcNow
        };

        unitOfWork.Begin();
        try
        {
            await userRepository.UpdateAsync(
                updatedUser, unitOfWork.Connection, unitOfWork.Transaction!);

            if (dto.RoleIds is not null)
            {
                await userRepository.UpdateUserRolesAsync(
                    dto.Id, dto.RoleIds, unitOfWork.Connection, unitOfWork.Transaction!);
            }

            unitOfWork.Commit();
        }
        catch
        {
            unitOfWork.Rollback();
            throw;
        }
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var existingUser = await userRepository.GetByIdAsync(id);
        if (existingUser is null) return;

        unitOfWork.Begin();
        try
        {
            // Clear roles before deleting to avoid constraint violations if cascading isn't on
            await userRepository.UpdateUserRolesAsync(
                id, Enumerable.Empty<Guid>(), unitOfWork.Connection, unitOfWork.Transaction!);
            await userRepository.DeleteAsync(
                id, unitOfWork.Connection, unitOfWork.Transaction!);

            unitOfWork.Commit();
        }
        catch
        {
            unitOfWork.Rollback();
            throw;
        }
    }

    private static UserDto MapToDto(User user) =>
        new(
            user.Id,
            user.Username,
            user.Email,
            user.IsActive,
            user.CreatedAt,
            user.Roles?.Select(r => r.Name) ?? []
        );
}
