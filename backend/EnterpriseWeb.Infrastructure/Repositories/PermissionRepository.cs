namespace EnterpriseWeb.Infrastructure.Repositories;

using Dapper;
using EnterpriseWeb.Domain.Entities;
using EnterpriseWeb.Domain.Interfaces;
using EnterpriseWeb.Infrastructure.Data;
using System.Data;

public class PermissionRepository(DapperContext context) : IPermissionRepository
{
    public async Task<IEnumerable<Permission>> GetAllAsync()
    {
        const string sql = "SELECT * FROM [dbo].[Permissions] ORDER BY [Module], [Code]";
        using IDbConnection conn = context.CreateConnection();
        return await conn.QueryAsync<Permission>(sql);
    }

    public async Task<IEnumerable<Permission>> GetByRoleIdAsync(Guid roleId)
    {
        const string sql = """
            SELECT p.* 
            FROM [dbo].[Permissions] p
            INNER JOIN [dbo].[RolePermissions] rp ON rp.[PermissionId] = p.[Id]
            WHERE rp.[RoleId] = @RoleId
            """;
        using IDbConnection conn = context.CreateConnection();
        return await conn.QueryAsync<Permission>(sql, new { RoleId = roleId });
    }
}
