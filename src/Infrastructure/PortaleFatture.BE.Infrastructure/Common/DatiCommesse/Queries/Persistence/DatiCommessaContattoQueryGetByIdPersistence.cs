using System.Data;
using PortaleFatture.BE.Core.Entities.DatiCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Queries.Persistence;

public class DatiCommessaContattoQueryGetByIdPersistence : DapperBase, IQuery<IEnumerable<DatiCommessaContatto>>
{
    private readonly long _id;
    private static readonly string _sqlSelect = @"
SELECT fkiddaticommessa,
       email,
       tipo
FROM  pfw.daticommessacontatti 
WHERE fkiddaticommessa=@id";

    public DatiCommessaContattoQueryGetByIdPersistence(long id)
    {
        this._id = id;
    }
    public async Task<IEnumerable<DatiCommessaContatto>> Execute(IDbConnection? connection, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        return await ((IDatabase)this).SelectAsync<DatiCommessaContatto>(connection!, _sqlSelect, new { id = _id }, transaction); 
    } 
} 