using System.Data;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;

public class DatiFatturazioneQueryGetByDescrizionePersistence(IPortaleFattureOptions options, string? descrizione, string? prodotto, string? profilo) : DapperBase, IQuery<IEnumerable<DatiFatturazioneEnteDto>?>
{
    private readonly IPortaleFattureOptions _options = options;
    private readonly string? _descrizione = descrizione;
    private readonly string? _prodotto = prodotto;
    private readonly string? _profilo = profilo;
    private static readonly string _sqlSelect = DatiFatturazioneSQLBuilder.SelectByDescrizione();

    public async Task<IEnumerable<DatiFatturazioneEnteDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var sql = _sqlSelect.Add(schema);
        sql = sql.AddJoin(_options.SelfCareSchema!); 
        sql += EnteSQLBuilder.AddSearch(_prodotto, _profilo);
        return await ((IDatabase)this).SelectAsync<DatiFatturazioneEnteDto>(connection!, sql, new { 
            descrizione = _descrizione,
            prodotto = _prodotto,
            profilo = _profilo
        }, transaction); 
    }
}