namespace EnterpriseWeb.Domain.Interfaces;

using EnterpriseWeb.Domain.Entities;
using System.Data;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id);
    Task<IEnumerable<Role>> GetAllAsync();
    Task<Guid> CreateAsync(Role role, IDbConnection conn, IDbTransaction tx);
    Task UpdateAsync(Role role, IDbConnection conn, IDbTransaction tx);
    Task DeleteAsync(Guid id, IDbConnection conn, IDbTransaction tx);
    Task UpdateRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, IDbConnection conn, IDbTransaction tx);
}
