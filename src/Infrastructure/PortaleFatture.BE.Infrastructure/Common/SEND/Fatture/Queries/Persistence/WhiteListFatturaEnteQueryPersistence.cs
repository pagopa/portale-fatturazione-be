using System.Data;
using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public sealed class WhiteListFatturaEnteQueryPersistence(WhiteListFatturaEnteQuery command) : DapperBase, IQuery<WhiteListFatturaEnteDto?>
{
    private readonly WhiteListFatturaEnteQuery _command = command;
    private static readonly string _sqlSelectAll = FattureQueryRicercaBuilder.SelectWhiteList();
    private static readonly string _orderBy = FattureQueryRicercaBuilder.OrderByWhiteList();
    private static readonly string _sqlSelectAllCount = FattureQueryRicercaBuilder.SelectWhiteListCount();
    private static readonly string _offSet = FattureQueryRicercaBuilder.OffSet();
    public async Task<WhiteListFatturaEnteDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {

        var Whitelist = new WhiteListFatturaEnteDto();
        var where = string.Empty;

        var page = _command.Page;
        var size = _command.Size;
        where += " WHERE DataFine is null";

        if (!_command.IdEnti!.IsNullNotAny())
            where += $" AND e.InternalIstitutionId IN @identi";

        if (_command.TipologiaContratto.HasValue)
            where += $" AND c.FkIdTipoContratto = @tipocontratto";

        if (_command.Anno.HasValue)
            where += $" AND Anno = @anno";

        if (!_command.Mesi.IsNullNotAny())
            where += $" AND Mese IN @mesi";


        if (!string.IsNullOrEmpty(_command.TipologiaFattura))
            where += $" AND w.FkTipologiaFattura = @tipologiafattura";

        var orderBy = _orderBy;

        var sqlEnte = _sqlSelectAll;
        var sqlCount = _sqlSelectAllCount;
        if (page == null && size == null)
            sqlEnte += where + orderBy;
        else
            sqlEnte += where + orderBy + _offSet;

        sqlCount += where;
        var sql = string.Join(";", sqlEnte, sqlCount);

        var query = new
        {
            Size = size,
            Page = page,
            IdEnti = _command.IdEnti,
            Tipocontratto = _command.TipologiaContratto,
            Anno = _command.Anno,
            Mesi = _command.Mesi,
            TipologiaFattura = _command.TipologiaFattura
        };


        using (var values = await ((IDatabase)this).QueryMultipleAsync<SimpleWhiteListFatturaEnteDto>(
            connection!,
            sql,
            query,
            transaction,
            CommandType.Text,
            null,
            CommandFlags.NoCache))
        {
            Whitelist.Whitelist = await values.ReadAsync<SimpleWhiteListFatturaEnteDto>();
            Whitelist.Count = await values.ReadFirstAsync<int>();
        }

        return Whitelist;
    }
}