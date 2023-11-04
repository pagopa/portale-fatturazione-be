using System.Data;
using PortaleFatture.BE.Core.Entities.DatiCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Queries.Persistence;

public class DatiCommessaQueryGetByIdPersistence : DapperBase, IQuery<DatiCommessa?>
{
    private readonly long _id;
    private static readonly string _sqlSelect = @"
SELECT iddaticommessa as id,
       cup,
       cig,
       codcommessa,
       datadocumento,
       splitpayment,
       fkidente as idente,
       fkidtipocontratto as idtipocontratto,
       iddocumento,
       datacreazione,
       datamodifica,
       [map],
       flagordinecontratto
FROM   pfw.daticommessa
WHERE iddaticommessa=@id";

    public DatiCommessaQueryGetByIdPersistence(long id)
    {
        this._id = id; 
    }


    public async Task<DatiCommessa?> Execute(IDbConnection? connection, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ((IDatabase)this).SingleAsync<DatiCommessa>(connection!, _sqlSelect, new { id = _id }, transaction);
        }
        catch
        {
            return null;
        }
    }
} 