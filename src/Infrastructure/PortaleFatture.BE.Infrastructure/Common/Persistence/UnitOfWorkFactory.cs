using System.Data;
using Microsoft.Data.SqlClient;

namespace PortaleFatture.BE.Infrastructure.Common.Persistence;

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly string _connectionString; 
    public UnitOfWorkFactory(string connectionString)
    => _connectionString = connectionString;
    public async Task<IUnitOfWork> Create(bool transactional = false, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken ct = default)
    {
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return new UnitOfWork(conn, transactional, isolationLevel);
    }
} 