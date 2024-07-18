using System.Data;
using PortaleFatture.BE.Core.Exceptions;

namespace PortaleFatture.BE.Infrastructure.Common.Persistence;

public class DbContext : IDbContext
{
    private bool _disposed;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private string _schema;
    public DbContext(IDbConnection connection,
                  string schema,
                  bool transactional = false,
                  IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        if (string.IsNullOrEmpty(schema) || schema!.Length > 4)
            throw new ConfigurationException("Database invalid schema!");

        _schema = schema.EndsWith(".") ? schema : $"{schema}.";
        _connection = connection;
        if (_connection.State == ConnectionState.Closed)
            _connection.Open();
        if (transactional)
            _transaction = _connection.BeginTransaction(isolationLevel);
    }

    public string GetSchema() => _schema;

    public Task<T> Query<T>(IQuery<T> query, CancellationToken ct = default)
        => query.Execute(_connection, _schema, _transaction, ct);


    public Task<int> Execute(ICommand command, CancellationToken ct = default)
    {
        if (command.RequiresTransaction && _transaction == null)
            throw new Exception($"The command {command.GetType()} requires a transaction");

        return command.Execute(_connection, _schema, _transaction, ct);
    }

    public Task<T> Execute<T>(ICommand<T> command, CancellationToken ct = default)
    {
        if (command.RequiresTransaction && _transaction == null)
            throw new Exception($"The command {command.GetType()} requires a transaction");

        return command.Execute(_connection, _schema, _transaction, ct);
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

    ~DbContext()
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