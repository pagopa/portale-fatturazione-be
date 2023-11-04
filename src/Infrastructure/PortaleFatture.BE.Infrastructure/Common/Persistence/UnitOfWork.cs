using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private bool _disposed;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;

    public UnitOfWork(IDbConnection connection,
                      TransactionCommand? transaction = null)
    {
        _connection = connection;
        if (_connection.State == ConnectionState.Closed)
            _connection.Open();
        if (transaction is not null && transaction.Create)
            _transaction = _connection.BeginTransaction(transaction.Level);
    }

    public UnitOfWork(IDbConnection connection,
                  bool transactional = false,
                  IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        _connection = connection;
        if (_connection.State == ConnectionState.Closed)
            _connection.Open();
        if (transactional)
            _transaction = _connection.BeginTransaction(isolationLevel);
    }

    public Task<T> Query<T>(IQuery<T> query, CancellationToken ct = default)
        => query.Execute(_connection, _transaction, ct);


    public Task<int> Execute(ICommand command, CancellationToken ct = default)
    {
        if (command.RequiresTransaction && _transaction == null)
            throw new Exception($"The command {command.GetType()} requires a transaction");

        return command.Execute(_connection, _transaction, ct);
    }

    public Task<T> Execute<T>(ICommand<T> command, CancellationToken ct = default)
    {
        if (command.RequiresTransaction && _transaction == null)
            throw new Exception($"The command {command.GetType()} requires a transaction");

        return command.Execute(_connection, _transaction, ct);
    }

    public void Commit()
        => _transaction?.Commit();

    public void Rollback()
        => _transaction?.Rollback();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~UnitOfWork()
        => Dispose(false);

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _transaction?.Dispose();
            _connection?.Dispose();
        }

        _transaction = null;
        _connection = null;

        _disposed = true;
    }
}