using System.Windows.Input;

namespace PortaleFatture.BE.Infrastructure.Common.Persistence;

public interface IDbContext : IDisposable
{
    public string GetSchema();
    Task<T> Query<T>(IQuery<T> query, CancellationToken ct = default);
    Task<int> Execute(ICommand command, CancellationToken ct = default);
    Task<T> Execute<T>(ICommand<T> command, CancellationToken ct = default);
    void Commit();
    void Rollback();
}