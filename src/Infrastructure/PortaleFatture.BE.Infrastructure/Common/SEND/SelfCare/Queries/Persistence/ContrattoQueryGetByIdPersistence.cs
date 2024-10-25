using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence;

public class ContrattoQueryGetByIdPersistence(string idEnte, string? prodotto) : DapperBase, IQuery<Contratto?>
{
    private readonly string? _idEnte = idEnte;
    private readonly string? _prodotto = prodotto;
    private static readonly string _sqlSelect = ContrattoSQLBuilder.SelectBy();

    public async Task<Contratto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var results = await ((IDatabase)this).SelectAsync<Contratto>(connection!, _sqlSelect.Add(schema),
            new
            {
                IdEnte = _idEnte,
                Prodotto = _prodotto
            }, transaction);
        return results?.FirstOrDefault();
    }
}