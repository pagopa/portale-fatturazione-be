using System.Data;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.Persistence;

public class DbContextFactory : IDbContextFactory, IFattureDbContextFactory, ISelfCareDbContextFactory
{
    private readonly string _schema;
    private readonly string _connectionString;
    public DbContextFactory(string connectionString, string schema)
    {
        _connectionString = connectionString;
        _schema = schema;
    }
    public async Task<IDbContext> Create(
        bool transactional = false, 
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, 
        CancellationToken ct = default)
    {
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return new DbContext(conn, _schema, transactional, isolationLevel);
    }
}