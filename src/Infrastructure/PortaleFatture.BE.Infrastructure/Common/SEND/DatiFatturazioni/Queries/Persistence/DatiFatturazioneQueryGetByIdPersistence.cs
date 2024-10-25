using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.DatiFatturazioni;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries.Persistence;

public class DatiFatturazioneQueryGetByIdPersistence : DapperBase, IQuery<DatiFatturazione?>
{
    private readonly long _id;
    private static readonly string _sqlSelect = DatiFatturazioneSQLBuilder.SelectById();

    public DatiFatturazioneQueryGetByIdPersistence(long id)
    {
        _id = id;
    }
    public async Task<DatiFatturazione?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ((IDatabase)this).SingleAsync<DatiFatturazione>(connection!, _sqlSelect.Add(schema), new { id = _id }, transaction);
        }
        catch
        {
            return null;
        }
    }
}