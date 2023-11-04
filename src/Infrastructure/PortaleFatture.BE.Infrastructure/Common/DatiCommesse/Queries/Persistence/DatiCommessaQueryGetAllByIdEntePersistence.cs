using System.Data;
using PortaleFatture.BE.Core.Entities.DatiCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Queries.Persistence;

public class DatiCommessaQueryGetAllByIdEntePersistence : DapperBase, IQuery<IEnumerable<DatiCommessa>?>
{
    private readonly string _idEnte;
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
WHERE fkidente=@idente";

    public DatiCommessaQueryGetAllByIdEntePersistence(string  idEnte)
    {
        this._idEnte = idEnte;
    } 

    public async Task<IEnumerable<DatiCommessa>?> Execute(IDbConnection? connection, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        return await ((IDatabase)this).SelectAsync<DatiCommessa>(connection!, _sqlSelect, new { idente = _idEnte }, transaction); 
    }
}