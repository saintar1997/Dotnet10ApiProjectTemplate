namespace EnterpriseWeb.Infrastructure.Repositories;

using Dapper;
using EnterpriseWeb.Domain.Entities;
using EnterpriseWeb.Domain.Interfaces;
using EnterpriseWeb.Infrastructure.Data;
using System.Data;

public class MenuRepository(DapperContext context) : IMenuRepository
{
    public async Task<Menu?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT m.*, p.[Code]
            FROM [dbo].[Menus] m
            LEFT JOIN [dbo].[MenuPermissions] mp ON mp.[MenuId] = m.[Id]
            LEFT JOIN [dbo].[Permissions] p ON p.[Id] = mp.[PermissionId]
            WHERE m.[Id] = @Id
            """;

        using IDbConnection conn = context.CreateConnection();
        Menu? result = null;
        var perms = new List<string>();

        await conn.QueryAsync<Menu, string, Menu>(
            sql,
            (menu, permCode) =>
            {
                result ??= menu;
                if (permCode is not null)
                    perms.Add(permCode);
                return menu;
            },
            new { Id = id },
            splitOn: "Code"
        );

        return result is null ? null : result with { RequiredPermissions = perms };
    }

    public async Task<IEnumerable<Menu>> GetAllAsync()
    {
        const string sql = """
            SELECT m.*, p.[Code]
            FROM [dbo].[Menus] m
            LEFT JOIN [dbo].[MenuPermissions] mp ON mp.[MenuId] = m.[Id]
            LEFT JOIN [dbo].[Permissions] p ON p.[Id] = mp.[PermissionId]
            ORDER BY m.[SortOrder]
            """;

        return await QueryMenusWithPermissions(sql, null);
    }

    public async Task<IEnumerable<Menu>> GetMenusByUserPermissionsAsync(IEnumerable<string> permissionCodes)
    {
        var codes = permissionCodes.ToList();
        if (codes.Count == 0)
            return [];

        const string sql = """
            SELECT DISTINCT m.*, p.[Code]
            FROM [dbo].[Menus] m
            LEFT JOIN [dbo].[MenuPermissions] mp ON mp.[MenuId] = m.[Id]
            LEFT JOIN [dbo].[Permissions] p ON p.[Id] = mp.[PermissionId]
            WHERE m.[IsVisible] = 1
              AND (
                    NOT EXISTS (SELECT 1 FROM [dbo].[MenuPermissions] WHERE [MenuId] = m.[Id])
                    OR m.[Id] IN (
                        SELECT mp2.[MenuId]
                        FROM [dbo].[MenuPermissions] mp2
                        INNER JOIN [dbo].[Permissions] p2 ON p2.[Id] = mp2.[PermissionId]
                        WHERE p2.[Code] IN @Codes
                    )
              )
            ORDER BY m.[SortOrder]
            """;

        return await QueryMenusWithPermissions(sql, new { Codes = codes });
    }

    public async Task<Guid> CreateAsync(Menu entity, IDbConnection conn, IDbTransaction tx)
    {
        const string sql = """
            INSERT INTO [dbo].[Menus] ([Id], [ParentId], [Title], [Path], [Icon], [SortOrder], [IsVisible], [CreatedAt])
            VALUES (@Id, @ParentId, @Title, @Path, @Icon, @SortOrder, @IsVisible, @CreatedAt)
            """;

        var id = Guid.NewGuid();
        await conn.ExecuteAsync(sql, new
        {
            Id = id,
            entity.ParentId,
            entity.Title,
            entity.Path,
            entity.Icon,
            entity.SortOrder,
            entity.IsVisible,
            CreatedAt = DateTime.UtcNow
        }, tx);
        return id;
    }

    public async Task UpdateAsync(Menu entity, IDbConnection conn, IDbTransaction tx)
    {
        const string sql = """
            UPDATE [dbo].[Menus]
            SET [ParentId]  = @ParentId,
                [Title]     = @Title,
                [Path]      = @Path,
                [Icon]      = @Icon,
                [SortOrder] = @SortOrder,
                [IsVisible] = @IsVisible
            WHERE [Id] = @Id
            """;

        await conn.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.ParentId,
            entity.Title,
            entity.Path,
            entity.Icon,
            entity.SortOrder,
            entity.IsVisible
        }, tx);
    }

    public async Task DeleteAsync(Guid id, IDbConnection conn, IDbTransaction tx)
    {
        // ลบ MenuPermissions ก่อน
        const string deletePermsSql = "DELETE FROM [dbo].[MenuPermissions] WHERE [MenuId] = @Id";
        await conn.ExecuteAsync(deletePermsSql, new { Id = id }, tx);

        // ลบ Menu
        const string deleteMenuSql = "DELETE FROM [dbo].[Menus] WHERE [Id] = @Id";
        await conn.ExecuteAsync(deleteMenuSql, new { Id = id }, tx);
    }

    public async Task UpdateMenuPermissionsAsync(
        Guid menuId, IEnumerable<string> permissionCodes, IDbConnection conn, IDbTransaction tx)
    {
        // 1. ลบ permissions เดิม
        const string deleteSql = "DELETE FROM [dbo].[MenuPermissions] WHERE [MenuId] = @MenuId";
        await conn.ExecuteAsync(deleteSql, new { MenuId = menuId }, tx);

        // 2. Insert permissions ใหม่โดย lookup PermissionId จาก Code
        var codes = permissionCodes.ToList();
        if (codes.Count > 0)
        {
            const string insertSql = """
                INSERT INTO [dbo].[MenuPermissions] ([MenuId], [PermissionId])
                SELECT @MenuId, p.[Id]
                FROM [dbo].[Permissions] p
                WHERE p.[Code] = @Code
                """;

            foreach (var code in codes)
            {
                await conn.ExecuteAsync(insertSql, new { MenuId = menuId, Code = code }, tx);
            }
        }
    }

    private async Task<IEnumerable<Menu>> QueryMenusWithPermissions(string sql, object? param)
    {
        using IDbConnection conn = context.CreateConnection();
        var menuDict = new Dictionary<Guid, (Menu Menu, List<string> Perms)>();

        await conn.QueryAsync<Menu, string, Menu>(
            sql,
            (menu, permCode) =>
            {
                if (!menuDict.TryGetValue(menu.Id, out var entry))
                {
                    entry = (menu, []);
                    menuDict[menu.Id] = entry;
                }

                if (permCode is not null)
                    entry.Perms.Add(permCode);

                return menu;
            },
            param,
            splitOn: "Code"
        );

        return menuDict.Values.Select(e => e.Menu with { RequiredPermissions = e.Perms });
    }
}
