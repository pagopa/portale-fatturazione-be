using System.Data;
using PortaleFatture.BE.Core.Entities.DatiCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Queries.Persistence;

public class TipoContrattoQueryGetAllPersistence : DapperBase, IQuery<IEnumerable<TipoContratto>>
{
    private static readonly string _sqlSelect = @"
SELECT idtipocontratto as id,
       descrizione
FROM   pfw.tipocontratto;";
    public TipoContrattoQueryGetAllPersistence()
    {

    }

    public async Task<IEnumerable<TipoContratto>> Execute(IDbConnection? connection, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<TipoContratto>(connection!, _sqlSelect, transaction);
    }
}