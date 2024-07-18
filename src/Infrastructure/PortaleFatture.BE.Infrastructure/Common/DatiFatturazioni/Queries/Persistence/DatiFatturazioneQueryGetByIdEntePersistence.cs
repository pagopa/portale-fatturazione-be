using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;

public class DatiFatturazioneQueryGetByIdEntePersistence : DapperBase, IQuery<DatiFatturazione?>
{
    private readonly string _idEnte;
    private static readonly string _sqlSelect = DatiFatturazioneSQLBuilder.SelectByIdEnte(); 
    public DatiFatturazioneQueryGetByIdEntePersistence(string idEnte)
    {
        this._idEnte = idEnte;
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