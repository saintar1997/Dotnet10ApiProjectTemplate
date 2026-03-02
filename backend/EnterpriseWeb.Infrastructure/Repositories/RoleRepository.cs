namespace EnterpriseWeb.Infrastructure.Repositories;

using Dapper;
using EnterpriseWeb.Domain.Entities;
using EnterpriseWeb.Domain.Interfaces;
using EnterpriseWeb.Infrastructure.Data;
using System.Data;

public class RoleRepository(DapperContext context) : IRoleRepository
{
    public async Task<Role?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT r.*, p.[Id], p.[Code], p.[Module], p.[Description], p.[CreatedAt]
            FROM [dbo].[Roles] r
            LEFT JOIN [dbo].[RolePermissions] rp ON rp.[RoleId] = r.[Id]
            LEFT JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId]
            WHERE r.[Id] = @Id
            """;

        return await QueryRoleWithPermissionsPattern(sql, new { Id = id });
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        const string sql = """
            SELECT r.*, p.[Id], p.[Code], p.[Module], p.[Description], p.[CreatedAt]
            FROM [dbo].[Roles] r
            LEFT JOIN [dbo].[RolePermissions] rp ON rp.[RoleId] = r.[Id]
            LEFT JOIN [dbo].[Permissions] p ON p.[Id] = rp.[PermissionId]
            ORDER BY r.[Name] ASC
            """;

        using IDbConnection conn = context.CreateConnection();
        var roleDict = new Dictionary<Guid, Role>();

        await conn.QueryAsync<Role, Permission, Role>(
            sql,
            (role, permission) =>
            {
                if (!roleDict.TryGetValue(role.Id, out var existing))
                {
                    existing = role with { Permissions = [] };
                    roleDict[role.Id] = existing;
                }

                if (permission is not null)
                {
                    var perms = existing.Permissions.ToList();
                    perms.Add(permission);
                    roleDict[role.Id] = existing with { Permissions = perms };
                }

                return existing;
            },
            splitOn: "Id"
        );

        return roleDict.Values;
    }

    public async Task<Guid> CreateAsync(Role role, IDbConnection conn, IDbTransaction tx)
    {
        return await CreateAsyncInternal(role, conn, tx);
    }

    private static async Task<Guid> CreateAsyncInternal(Role role, IDbConnection conn, IDbTransaction? tx)
    {
        const string sql = """
            INSERT INTO [dbo].[Roles] ([Id], [Name], [Description], [CreatedAt])
            VALUES (@Id, @Name, @Description, @CreatedAt)
            """;

        var id = Guid.NewGuid();
        await conn.ExecuteAsync(sql, new
        {
            Id = id,
            role.Name,
            role.Description,
            CreatedAt = DateTime.UtcNow
        }, tx);
        return id;
    }

    public async Task UpdateAsync(Role role, IDbConnection conn, IDbTransaction tx)
    {
        await UpdateAsyncInternal(role, conn, tx);
    }

    private static async Task UpdateAsyncInternal(Role role, IDbConnection conn, IDbTransaction? tx)
    {
        const string sql = """
            UPDATE [dbo].[Roles]
            SET [Name] = @Name,
                [Description] = @Description
            WHERE [Id] = @Id
            """;

        await conn.ExecuteAsync(sql, new
        {
            role.Id,
            role.Name,
            role.Description
        }, tx);
    }

    public async Task DeleteAsync(Guid id, IDbConnection conn, IDbTransaction tx)
    {
        // 1. ลบ RolePermissions ก่อน
        const string deletePermsSql = "DELETE FROM [dbo].[RolePermissions] WHERE [RoleId] = @Id";
        await conn.ExecuteAsync(deletePermsSql, new { Id = id }, tx);

        // 2. ลบ UserRoles ที่ผูกกับ Role นี้
        const string deleteUserRolesSql = "DELETE FROM [dbo].[UserRoles] WHERE [RoleId] = @Id";
        await conn.ExecuteAsync(deleteUserRolesSql, new { Id = id }, tx);

        // 3. ลบ Role
        const string deleteRoleSql = "DELETE FROM [dbo].[Roles] WHERE [Id] = @Id";
        await conn.ExecuteAsync(deleteRoleSql, new { Id = id }, tx);
    }

    public async Task UpdateRolePermissionsAsync(
        Guid roleId, IEnumerable<Guid> permissionIds, IDbConnection conn, IDbTransaction tx)
    {
        await UpdateRolePermissionsAsyncInternal(roleId, permissionIds, conn, tx);
    }

    private static async Task UpdateRolePermissionsAsyncInternal(
        Guid roleId, IEnumerable<Guid> permissionIds, IDbConnection conn, IDbTransaction tx)
    {
        // First drop existing permissions
        const string deleteSql = "DELETE FROM [dbo].[RolePermissions] WHERE [RoleId] = @RoleId";
        await conn.ExecuteAsync(deleteSql, new { RoleId = roleId }, tx);

        // Then insert new ones
        if (permissionIds != null && permissionIds.Any())
        {
            const string insertSql = "INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES (@RoleId, @PermissionId)";
            var inserts = permissionIds.Select(pid => new { RoleId = roleId, PermissionId = pid });
            await conn.ExecuteAsync(insertSql, inserts, tx);
        }
    }

    private async Task<Role?> QueryRoleWithPermissionsPattern(string sql, object param)
    {
        using IDbConnection conn = context.CreateConnection();
        Role? result = null;
        var permissions = new List<Permission>();

        await conn.QueryAsync<Role, Permission, Role>(
            sql,
            (role, permission) =>
            {
                result ??= role;
                if (permission is not null)
                    permissions.Add(permission);
                return role;
            },
            param,
            splitOn: "Id"
        );

        return result is null ? null : result with { Permissions = permissions };
    }
}
