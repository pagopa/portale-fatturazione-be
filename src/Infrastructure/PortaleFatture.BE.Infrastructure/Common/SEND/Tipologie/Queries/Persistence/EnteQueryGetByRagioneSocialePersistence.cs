using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

public class EnteQueryGetByRagioneSocialePersistence(string descrizione, string? prodotto, string? profilo) : DapperBase, IQuery<IEnumerable<string>>
{
    private static readonly string _sqlSelect = EnteSQLBuilder.SelectAllBySearch();
    private readonly string _descrizione = descrizione;
    private readonly string? _prodotto = prodotto;
    private readonly string? _profilo = profilo;

    public async Task<IEnumerable<string>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var sql = _sqlSelect.Add(schema);
        sql += EnteSQLBuilder.AddSearch(_prodotto, _profilo);
        return await ((IDatabase)this).SelectAsync<string>(connection!, sql, new
        {
            descrizione = _descrizione,
            profilo = _profilo,
            prodotto = _prodotto
        }, transaction);
    }
}