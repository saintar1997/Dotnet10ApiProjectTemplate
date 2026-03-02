namespace EnterpriseWeb.Domain.Interfaces;

using EnterpriseWeb.Domain.Entities;
using System.Data;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(Guid id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<Guid> CreateAsync(User user, string passwordHash, IDbConnection conn, IDbTransaction tx);
    Task UpdateAsync(User user, IDbConnection conn, IDbTransaction tx);
    Task DeleteAsync(Guid id, IDbConnection conn, IDbTransaction tx);
    Task<IEnumerable<string>> GetPermissionCodesAsync(Guid userId);
    Task UpdateUserRolesAsync(Guid userId, IEnumerable<Guid> roleIds, IDbConnection conn, IDbTransaction tx);
}
