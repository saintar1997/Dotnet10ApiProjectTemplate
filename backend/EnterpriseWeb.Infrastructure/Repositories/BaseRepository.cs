namespace EnterpriseWeb.Infrastructure.Repositories;

using Dapper;
using EnterpriseWeb.Domain.Interfaces;
using EnterpriseWeb.Infrastructure.Data;
using System.Data;

public abstract class BaseRepository<T>(DapperContext context) : IBaseRepository<T> where T : class
{
    protected readonly DapperContext Context = context;

    protected async Task<IEnumerable<TResult>> QueryAsync<TResult>(string sql, object? param = null)
    {
        using IDbConnection conn = Context.CreateConnection();
        return await conn.QueryAsync<TResult>(sql, param);
    }

    protected async Task<TResult?> QueryFirstOrDefaultAsync<TResult>(string sql, object? param = null)
    {
        using IDbConnection conn = Context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<TResult>(sql, param);
    }

    protected async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        using IDbConnection conn = Context.CreateConnection();
        return await conn.ExecuteAsync(sql, param);
    }

    protected async Task<TResult?> ExecuteScalarAsync<TResult>(string sql, object? param = null)
    {
        using IDbConnection conn = Context.CreateConnection();
        return await conn.ExecuteScalarAsync<TResult>(sql, param);
    }

    public abstract Task<T?> GetByIdAsync(Guid id);
    public abstract Task<IEnumerable<T>> GetAllAsync();
    public abstract Task<Guid> CreateAsync(T entity);
    public abstract Task UpdateAsync(T entity);

    public virtual async Task DeleteAsync(Guid id)
    {
        var tableName = typeof(T).Name + "s";
        var sql = $"DELETE FROM [{tableName}] WHERE [Id] = @Id";
        await ExecuteAsync(sql, new { Id = id });
    }
}
