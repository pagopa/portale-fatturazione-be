using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;
using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

public class ProdottoQueryGetAllPersistence : DapperBase, IQuery<IEnumerable<Prodotto>>
{
    private static readonly string _sqlSelect = ProdottoSQLBuilder.SelectAll();

    public async Task<IEnumerable<Prodotto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<Prodotto>(connection!, _sqlSelect.Add(schema), null, transaction);
    }
}