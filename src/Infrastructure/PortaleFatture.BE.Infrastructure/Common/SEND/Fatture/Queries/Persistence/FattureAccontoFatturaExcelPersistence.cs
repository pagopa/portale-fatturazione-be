using System.Data;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureAccontoFatturaExcelPersistence(FattureAccontoExcelQuery command) : DapperBase, IQuery<IEnumerable<FattureAccontoExcelDto>?>
{
    private readonly FattureAccontoExcelQuery _command = command;
    private static readonly string _sql = FattureAccontoFatturaExcelBuilder.SelectAcconto();
    public async Task<IEnumerable<FattureAccontoExcelDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var anno = _command.Anno;
        var mese = _command.Mese;
        var where = " where f.Anno= @anno and f.mese=@mese ";

        // FIX bug #960:
        // 1) usare l'alias corretto della vista (vFattureAcconto f), non un inesistente "t"
        // 2) usare += per concatenare, non = (altrimenti si perde la WHERE iniziale)
        if (!_command.IdEnti!.IsNullNotAny())
            where += " AND f.IdEnte in @IdEnti ";

        if (_command.FkIdTipoContratto.HasValue)
            where += " AND c.FkIdTipoContratto = @FkIdTipoContratto ";

        var sql = _sql + where;

        var query = new
        {
            Anno = anno,
            Mese = mese,
            _command.IdEnti,
            _command.FkIdTipoContratto,
            _command.FatturaInviata
        };

        return await ((IDatabase)this).SelectAsync<FattureAccontoExcelDto>(
        connection!,
        sql,
        query,
        transaction);
    }
}