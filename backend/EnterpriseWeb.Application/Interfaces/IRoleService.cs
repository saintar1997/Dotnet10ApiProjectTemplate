namespace EnterpriseWeb.Application.Interfaces;

using EnterpriseWeb.Application.DTOs.Role;

public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    Task<RoleDto?> GetRoleByIdAsync(Guid id);
    Task<Guid> CreateRoleAsync(CreateRoleDto dto);
    Task UpdateRoleAsync(UpdateRoleDto dto);
    Task DeleteRoleAsync(Guid id);
}
