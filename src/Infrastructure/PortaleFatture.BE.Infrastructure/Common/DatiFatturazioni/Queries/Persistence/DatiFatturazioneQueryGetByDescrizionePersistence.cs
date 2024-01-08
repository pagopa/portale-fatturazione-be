using System.Data;
using System.Data.SqlTypes;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;

public class DatiFatturazioneQueryGetByDescrizionePersistence(IPortaleFattureOptions options, string? descrizione, string? prodotto, string? profilo, int? top) : DapperBase, IQuery<IEnumerable<DatiFatturazioneEnteDto>?>
{
    private readonly IPortaleFattureOptions _options = options;
    private readonly string? _descrizione = descrizione;
    private readonly string? _prodotto = prodotto;
    private readonly string? _profilo = profilo;
    private readonly int? _top = top;
    private static readonly string _sqlSelect = DatiFatturazioneSQLBuilder.SelectByDescrizione();

    public async Task<IEnumerable<DatiFatturazioneEnteDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var sql = _sqlSelect.Add(schema);
        sql = sql.AddJoin(_options.SelfCareSchema!);
        sql += EnteSQLBuilder.AddSearch(_prodotto, _profilo);
        sql = sql.AddTop(_top);
        return await ((IDatabase)this).SelectAsync<DatiFatturazioneEnteDto>(connection!, sql, new
        {
            descrizione = _descrizione,
            prodotto = _prodotto,
            profilo = _profilo
        }, transaction);
    }
}