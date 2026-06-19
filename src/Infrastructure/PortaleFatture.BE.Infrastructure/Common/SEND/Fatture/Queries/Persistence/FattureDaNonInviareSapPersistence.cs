
using System.Data;
using Dapper;
using DocumentFormat.OpenXml.Drawing;
using MailKit.Search;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public sealed class FattureDaNonInviareSapPersistence(FattureDaNonInviareSapQuery command) : DapperBase, IQuery<FattureDaNonInviareSapDto?>
{
    private readonly FattureDaNonInviareSapQuery _command = command;
    private static readonly string _sqlSelectAll = FattureDaNonInviareSapBuilder.SelectEsclusioneFattureList();
    private static readonly string _orderBy = FattureDaNonInviareSapBuilder.OrderByEsclusioneFatture();
    private static readonly string _sqlSelectAllCount = FattureDaNonInviareSapBuilder.SelectEsclusioneFattureCount();
    private static readonly string _offSet = FattureDaNonInviareSapBuilder.OffSet();

    public async Task<FattureDaNonInviareSapDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var Staginglist = new FattureDaNonInviareSapDto();
        var where = string.Empty;

        var page = _command.Page;
        var size = _command.Size;
        where += " WHERE Stato <> 2";

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


        using (var values = await ((IDatabase)this).QueryMultipleAsync<SimpleFattureDaNonInviareSapDto>(
            connection!,
            sql,
            query,
            transaction,
            CommandType.Text,
            null,
            CommandFlags.NoCache))
        {
            Staginglist.FattureEscluse = await values.ReadAsync<SimpleFattureDaNonInviareSapDto>();
            Staginglist.Count = await values.ReadFirstAsync<int>();
        }

        return Staginglist;
    }
}

