namespace EnterpriseWeb.Application.Interfaces;

using EnterpriseWeb.Application.DTOs.Permission;

public interface IPermissionService
{
    Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync();
}
