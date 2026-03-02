namespace EnterpriseWeb.Application.Services;

using EnterpriseWeb.Application.DTOs.Permission;
using EnterpriseWeb.Application.Interfaces;
using EnterpriseWeb.Domain.Interfaces;

public class PermissionService(IPermissionRepository permissionRepository) : IPermissionService
{
    public async Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync()
    {
        var permissions = await permissionRepository.GetAllAsync();
        return permissions.Select(p => new PermissionDto(
            p.Id,
            p.Code,
            p.Module,
            p.Description
        ));
    }
}
