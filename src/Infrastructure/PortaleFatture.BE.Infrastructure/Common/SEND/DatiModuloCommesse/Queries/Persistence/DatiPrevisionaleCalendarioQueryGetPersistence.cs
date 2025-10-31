using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

public class DatiPrevisionaleCalendarioQueryGetPersistence(DatiPrevisionaleCalendarioQuery command) : DapperBase, IQuery<IEnumerable<DatiPrevisionaleCalendarioDto>>
{
    private readonly DatiPrevisionaleCalendarioQuery? _command = command;
    private static readonly string _sqlSelect = DatiModuloCommessaPrevisionaliSQLBuilder.SelectPrevisionaleByAnnoMese();

    public async Task<IEnumerable<DatiPrevisionaleCalendarioDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        return await ((IDatabase)this).SelectAsync<DatiPrevisionaleCalendarioDto>(connection!, _sqlSelect.Add(schema),
             _command,
             transaction);
    }
}