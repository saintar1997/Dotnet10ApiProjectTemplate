namespace EnterpriseWeb.Domain.Interfaces;

using EnterpriseWeb.Domain.Entities;
using System.Data;

public interface IMenuRepository
{
    Task<IEnumerable<Menu>> GetAllAsync();
    Task<IEnumerable<Menu>> GetMenusByUserPermissionsAsync(IEnumerable<string> permissionCodes);
    Task<Menu?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(Menu menu, IDbConnection conn, IDbTransaction tx);
    Task UpdateAsync(Menu menu, IDbConnection conn, IDbTransaction tx);
    Task DeleteAsync(Guid id, IDbConnection conn, IDbTransaction tx);
    Task UpdateMenuPermissionsAsync(Guid menuId, IEnumerable<string> permissionCodes, IDbConnection conn, IDbTransaction tx);
}

