namespace EnterpriseWeb.Application.Services;

using EnterpriseWeb.Application.DTOs.Permission;
using EnterpriseWeb.Application.DTOs.Role;
using EnterpriseWeb.Application.Interfaces;
using EnterpriseWeb.Domain.Entities;
using EnterpriseWeb.Domain.Interfaces;

public class RoleService(IRoleRepository roleRepository, IUnitOfWork unitOfWork) : IRoleService
{
    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        var roles = await roleRepository.GetAllAsync();
        return roles.Select(r => new RoleDto(
            r.Id,
            r.Name,
            r.Description,
            r.CreatedAt,
            r.Permissions.Select(p => new PermissionDto(p.Id, p.Code, p.Module, p.Description))
        ));
    }

    public async Task<RoleDto?> GetRoleByIdAsync(Guid id)
    {
        var r = await roleRepository.GetByIdAsync(id);
        if (r == null) return null;

        return new RoleDto(
            r.Id,
            r.Name,
            r.Description,
            r.CreatedAt,
            r.Permissions.Select(p => new PermissionDto(p.Id, p.Code, p.Module, p.Description))
        );
    }

    public async Task<Guid> CreateRoleAsync(CreateRoleDto dto)
    {
        var role = new Role
        {
            Name = dto.Name,
            Description = dto.Description
        };

        unitOfWork.Begin();
        try
        {
            var roleId = await roleRepository.CreateAsync(
                role, unitOfWork.Connection, unitOfWork.Transaction!);

            if (dto.PermissionIds != null && dto.PermissionIds.Any())
            {
                await roleRepository.UpdateRolePermissionsAsync(
                    roleId, dto.PermissionIds, unitOfWork.Connection, unitOfWork.Transaction!);
            }

            unitOfWork.Commit();
            return roleId;
        }
        catch
        {
            unitOfWork.Rollback();
            throw;
        }
    }

    public async Task UpdateRoleAsync(UpdateRoleDto dto)
    {
        var role = await roleRepository.GetByIdAsync(dto.Id);
        if (role == null) throw new KeyNotFoundException("Role not found");

        var updatedRole = role with
        {
            Name = dto.Name,
            Description = dto.Description
        };

        unitOfWork.Begin();
        try
        {
            await roleRepository.UpdateAsync(
                updatedRole, unitOfWork.Connection, unitOfWork.Transaction!);

            if (dto.PermissionIds != null)
            {
                await roleRepository.UpdateRolePermissionsAsync(
                    dto.Id, dto.PermissionIds, unitOfWork.Connection, unitOfWork.Transaction!);
            }

            unitOfWork.Commit();
        }
        catch
        {
            unitOfWork.Rollback();
            throw;
        }
    }

    public async Task DeleteRoleAsync(Guid id)
    {
        unitOfWork.Begin();
        try
        {
            await roleRepository.DeleteAsync(id, unitOfWork.Connection, unitOfWork.Transaction!);
            unitOfWork.Commit();
        }
        catch
        {
            unitOfWork.Rollback();
            throw;
        }
    }
}
