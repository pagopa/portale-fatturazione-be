using System.Windows.Input;

namespace PortaleFatture.BE.Infrastructure.Common.Persistence;

public interface IUnitOfWork : IDisposable
{
    Task<T> Query<T>(IQuery<T> query, CancellationToken cancellationToken = default);
    Task<int> Execute(ICommand command, CancellationToken cancellationToken = default);
    Task<T> Execute<T>(ICommand<T> command, CancellationToken cancellation = default);
    void Commit();
    void Rollback();
}