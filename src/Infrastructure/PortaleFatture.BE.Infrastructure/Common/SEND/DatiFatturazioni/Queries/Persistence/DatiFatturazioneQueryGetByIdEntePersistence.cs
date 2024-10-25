using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.DatiFatturazioni;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries.Persistence;

public class DatiFatturazioneQueryGetByIdEntePersistence : DapperBase, IQuery<DatiFatturazione?>
{
    private readonly string _idEnte;
    private static readonly string _sqlSelect = DatiFatturazioneSQLBuilder.SelectByIdEnte();
    public DatiFatturazioneQueryGetByIdEntePersistence(string idEnte)
    {
        _idEnte = idEnte;
    }

    public async Task<DatiFatturazione?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var values = await ((IDatabase)this).SelectAsync<DatiFatturazione>(connection!, _sqlSelect.Add(schema), new { idente = _idEnte }, transaction);
        if (values.IsNullNotAny())
            return null;
        if (values.Count() > 1)
            throw new DomainException($"Duplicate values in dati fatturazione for ente id: {_idEnte}");
        return values.FirstOrDefault();
    }
}