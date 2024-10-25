using System.Data;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries.Persistence;

public class DatiFatturazioneQueryGetByDescrizionePersistence(IPortaleFattureOptions options, string[]? idEnti, string? prodotto, string? profilo, int? top) : DapperBase, IQuery<IEnumerable<DatiFatturazioneEnteDto>?>
{
    private readonly IPortaleFattureOptions _options = options;
    private readonly string[]? _idEnti = idEnti;
    private readonly string? _prodotto = prodotto;
    private readonly string? _profilo = profilo;
    private readonly int? _top = top;
    private static readonly string _sqlSelect = DatiFatturazioneSQLBuilder.SelectByDescrizione(false);
    private static readonly string _sqlSelectAll = DatiFatturazioneSQLBuilder.SelectByDescrizione(true);
    public async Task<IEnumerable<DatiFatturazioneEnteDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        string? sql;
        if (_idEnti!.IsNullNotAny())
            sql = _sqlSelectAll.Add(schema);
        else
            sql = _sqlSelect.Add(schema);

        sql = sql.AddJoin(_options.SelfCareSchema!);
        sql += EnteSQLBuilder.AddSearch(_prodotto, _profilo);
        sql = sql.AddTop(_top);
        return await ((IDatabase)this).SelectAsync<DatiFatturazioneEnteDto>(connection!, sql, new
        {
            idEnti = _idEnti,
            prodotto = _prodotto,
            profilo = _profilo
        }, transaction);
    }
}