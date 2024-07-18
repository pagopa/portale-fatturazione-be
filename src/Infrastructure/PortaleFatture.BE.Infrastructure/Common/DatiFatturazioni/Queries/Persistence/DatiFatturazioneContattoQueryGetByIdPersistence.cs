using System.Data;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;

public class DatiFatturazioneContattoQueryGetByIdPersistence : DapperBase, IQuery<IEnumerable<DatiFatturazioneContatto>>
{
    private readonly long _id;
    private static readonly string _sqlSelect = DatiFatturazioneContattoSQLBuilder.SelectAllByIdDatiFatturazione();

    public DatiFatturazioneContattoQueryGetByIdPersistence(long id)
    {
        this._id = id;
    }
    public async Task<IEnumerable<DatiFatturazioneContatto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        return await ((IDatabase)this).SelectAsync<DatiFatturazioneContatto>(connection!, _sqlSelect.Add(schema), new { IdDatiFatturazione = _id }, transaction); 
    } 
} 