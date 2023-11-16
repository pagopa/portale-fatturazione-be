using System.Data;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;

public class DatiFatturazioneWithContattiQueryGetByIdPersistence : DapperBase, IQuery<DatiFatturazione?>
{
    private readonly long _id;
    private static readonly string _sqlSelect = String.Join(";", DatiFatturazioneSQLBuilder.SelectById(), DatiFatturazioneContattoSQLBuilder.SelectAllByIdDatiFatturazione());

    public DatiFatturazioneWithContattiQueryGetByIdPersistence(long id)
    {
        this._id = id;
    } 
    public async Task<DatiFatturazione?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        var values = await ((IDatabase)this).QueryMultipleAsync<DatiFatturazione>(connection!, _sqlSelect.Add(schema), new { id = _id , IdDatiFatturazione = _id }, transaction);
        if (values == null)
            return null;
        var datiFatturazione = await values.ReadFirstAsync<DatiFatturazione>();
        datiFatturazione.Contatti = await values.ReadAsync<DatiFatturazioneContatto>();
        return datiFatturazione;
    }
}