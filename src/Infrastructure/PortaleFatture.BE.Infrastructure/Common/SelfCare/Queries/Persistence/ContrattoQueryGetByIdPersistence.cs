using System.Data;
using PortaleFatture.BE.Core.Entities.SelfCare;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries.Persistence;

public class ContrattoQueryGetByIdPersistence : DapperBase, IQuery<Contratto?>
{
    private readonly string? _idEnte;
    private static readonly string _sqlSelect = ContrattoSQLBuilder.SelectBy();

    public ContrattoQueryGetByIdPersistence(string idEnte)
    {
        this._idEnte = idEnte;
    }
    public async Task<Contratto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var results = await ((IDatabase)this).SelectAsync<Contratto>(connection!, _sqlSelect.Add(schema), new { IdEnte = _idEnte }, transaction);
        return results?.FirstOrDefault();
    }
} 