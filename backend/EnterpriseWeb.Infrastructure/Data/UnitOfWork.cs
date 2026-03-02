namespace EnterpriseWeb.Infrastructure.Data;

using EnterpriseWeb.Application.Interfaces;
using System.Data;

public class UnitOfWork(DapperContext context) : IUnitOfWork
{
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    public IDbConnection Connection =>
        _connection ??= CreateAndOpenConnection();

    public IDbTransaction? Transaction => _transaction;

    public void Begin()
    {
        if (_transaction is not null)
            throw new InvalidOperationException("Transaction already started.");
        _transaction = Connection.BeginTransaction();
    }

    public void Commit()
    {
        _transaction?.Commit();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _transaction?.Dispose();
        _connection?.Dispose();
        _disposed = true;
    }

    private IDbConnection CreateAndOpenConnection()
    {
        var conn = context.CreateConnection();
        conn.Open();
        return conn;
    }
}
