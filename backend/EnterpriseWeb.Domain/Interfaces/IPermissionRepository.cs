namespace EnterpriseWeb.Domain.Interfaces;

using EnterpriseWeb.Domain.Entities;

public interface IPermissionRepository
{
    Task<IEnumerable<Permission>> GetAllAsync();
    Task<IEnumerable<Permission>> GetByRoleIdAsync(Guid roleId);
}
