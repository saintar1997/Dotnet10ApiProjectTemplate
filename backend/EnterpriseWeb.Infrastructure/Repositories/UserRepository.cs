namespace EnterpriseWeb.Infrastructure.Repositories;

using Dapper;
using EnterpriseWeb.Domain.Entities;
using EnterpriseWeb.Domain.Interfaces;
using EnterpriseWeb.Infrastructure.Data;
using System.Data;

public class UserRepository(DapperContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT u.*, r.[Id], r.[Name], r.[Description], r.[CreatedAt]
            FROM [dbo].[Users] u
            LEFT JOIN [dbo].[UserRoles] ur ON ur.[UserId] = u.[Id]
            LEFT JOIN [dbo].[Roles] r ON r.[Id] = ur.[RoleId]
            WHERE u.[Id] = @Id
            """;

        return await QueryUserWithRoles(sql, new { Id = id });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = """
            SELECT u.*, r.[Id], r.[Name], r.[Description], r.[CreatedAt]
            FROM [dbo].[Users] u
            LEFT JOIN [dbo].[UserRoles] ur ON ur.[UserId] = u.[Id]
            LEFT JOIN [dbo].[Roles] r ON r.[Id] = ur.[RoleId]
            WHERE u.[Username] = @Username
            """;

        return await QueryUserWithRoles(sql, new { Username = username });
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        const string sql = """
            SELECT u.*, r.[Id], r.[Name], r.[Description], r.[CreatedAt]
            FROM [dbo].[Users] u
            LEFT JOIN [dbo].[UserRoles] ur ON ur.[UserId] = u.[Id]
            LEFT JOIN [dbo].[Roles] r ON r.[Id] = ur.[RoleId]
            ORDER BY u.[CreatedAt] DESC
            """;

        using IDbConnection conn = context.CreateConnection();

        var userDict = new Dictionary<Guid, User>();

        await conn.QueryAsync<User, Role, User>(
            sql,
            (user, role) =>
            {
                if (!userDict.TryGetValue(user.Id, out var existing))
                {
                    existing = user with { Roles = [] };
                    userDict[user.Id] = existing;
                }

                if (role is not null)
                {
                    var roles = existing.Roles.ToList();
                    roles.Add(role);
                    userDict[user.Id] = existing with { Roles = roles };
                }

                return existing;
            },
            splitOn: "Id"
        );

        return userDict.Values;
    }

    public async Task<Guid> CreateAsync(User entity, string passwordHash, IDbConnection conn, IDbTransaction tx)
    {
        return await CreateAsyncInternal(entity, passwordHash, conn, tx);
    }

    private static async Task<Guid> CreateAsyncInternal(
        User entity, string passwordHash, IDbConnection conn, IDbTransaction? tx)
    {
        const string sql = """
            INSERT INTO [dbo].[Users] ([Id], [Username], [Email], [PasswordHash], [IsActive], [CreatedAt], [CreatedBy])
            VALUES (@Id, @Username, @Email, @PasswordHash, @IsActive, @CreatedAt, @CreatedBy)
            """;

        var id = Guid.NewGuid();
        await conn.ExecuteAsync(sql, new
        {
            Id = id,
            entity.Username,
            entity.Email,
            PasswordHash = passwordHash,
            entity.IsActive,
            CreatedAt = DateTime.UtcNow,
            entity.CreatedBy
        }, tx);
        return id;
    }

    public async Task DeleteAsync(Guid id, IDbConnection conn, IDbTransaction tx)
    {
        const string sql = "DELETE FROM [dbo].[Users] WHERE [Id] = @Id";
        await conn.ExecuteAsync(sql, new { Id = id }, tx);
    }

    public async Task UpdateAsync(User entity, IDbConnection conn, IDbTransaction tx)
    {
        await UpdateAsyncInternal(entity, conn, tx);
    }

    private static async Task UpdateAsyncInternal(User entity, IDbConnection conn, IDbTransaction? tx)
    {
        const string sql = """
            UPDATE [dbo].[Users]
            SET [Username]  = @Username,
                [Email]     = @Email,
                [IsActive]  = @IsActive,
                [UpdatedAt] = @UpdatedAt,
                [UpdatedBy] = @UpdatedBy
            WHERE [Id] = @Id
            """;

        await conn.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Username,
            entity.Email,
            entity.IsActive,
            UpdatedAt = DateTime.UtcNow,
            entity.UpdatedBy
        }, tx);
    }

    public async Task<IEnumerable<string>> GetPermissionCodesAsync(Guid userId)
    {
        const string sql = """
            SELECT DISTINCT p.[Code]
            FROM [dbo].[Permissions] p
            INNER JOIN [dbo].[RolePermissions] rp ON rp.[PermissionId] = p.[Id]
            INNER JOIN [dbo].[UserRoles] ur ON ur.[RoleId] = rp.[RoleId]
            WHERE ur.[UserId] = @UserId
            """;

        using IDbConnection conn = context.CreateConnection();
        return await conn.QueryAsync<string>(sql, new { UserId = userId });
    }

    private async Task<User?> QueryUserWithRoles(string sql, object param)
    {
        using IDbConnection conn = context.CreateConnection();

        User? result = null;
        var roles = new List<Role>();

        await conn.QueryAsync<User, Role, User>(
            sql,
            (user, role) =>
            {
                result ??= user;
                if (role is not null)
                    roles.Add(role);
                return user;
            },
            param,
            splitOn: "Id"
        );

        return result is null ? null : result with { Roles = roles };
    }

    public async Task UpdateUserRolesAsync(Guid userId, IEnumerable<Guid> roleIds, IDbConnection conn, IDbTransaction tx)
    {
        await UpdateUserRolesAsyncInternal(userId, roleIds, conn, tx);
    }

    private static async Task UpdateUserRolesAsyncInternal(
        Guid userId, IEnumerable<Guid> roleIds, IDbConnection conn, IDbTransaction tx)
    {
        // 1. Delete existing roles
        const string deleteSql = "DELETE FROM [dbo].[UserRoles] WHERE [UserId] = @UserId";
        await conn.ExecuteAsync(deleteSql, new { UserId = userId }, tx);

        // 2. Insert new roles
        if (roleIds.Any())
        {
            const string insertSql = "INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (@UserId, @RoleId)";
            var parameters = roleIds.Select(roleId => new { UserId = userId, RoleId = roleId });
            await conn.ExecuteAsync(insertSql, parameters, tx);
        }
    }
}
