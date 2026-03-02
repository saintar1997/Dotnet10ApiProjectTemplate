namespace EnterpriseWeb.Application.Interfaces;

using System.Data;

public interface IUnitOfWork : IDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
    void Begin();
    void Commit();
    void Rollback();
}
