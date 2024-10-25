using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

public class EnteQueryGetByDescrizionePersistence(string descrizione) : DapperBase, IQuery<IEnumerable<Ente>>
{
    private static readonly string _sqlSelect = EnteSQLBuilder.SelectAllByDescrizione();
    private readonly string _descrizione = descrizione;

    public async Task<IEnumerable<Ente>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var sql = _sqlSelect.Add(schema);

        return await ((IDatabase)this).SelectAsync<Ente>(connection!, sql, new
        {
            descrizione = _descrizione
        }, transaction);
    }
}