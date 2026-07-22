using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public sealed class GestioneFattureQueryPersistence(GestioneFattureQuery command) : DapperBase, IQuery<GestioneFattureListDto?>
{

    private readonly GestioneFattureQuery _command = command;
    private static readonly string _sqlSelectAll = GestioneFattureBuilder.SelectGestioneFattureList();
    private static readonly string _orderBy = GestioneFattureBuilder.OrderByGestioneFatture();
    private static readonly string _sqlSelectAllCount = GestioneFattureBuilder.SelectGestioneFattureCount();
    private static readonly string _offSet = GestioneFattureBuilder.OffSet();
    public async Task<GestioneFattureListDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {

        var Staginglist = new GestioneFattureListDto();
        var page = _command.Page;
        var size = _command.Size;

        var conditions = new List<string>();

        if (!_command.IdEnti!.IsNullNotAny())
            conditions.Add("Ente IN @identi");

        if (_command.TipologiaContratto.HasValue)
            conditions.Add("IdTipoContratto = @tipocontratto");

        if (_command.Anno.HasValue)
            conditions.Add("Anno = @anno");

        if (!_command.Mesi.IsNullNotAny())
            conditions.Add("Mese IN @mesi");

        if (!string.IsNullOrEmpty(_command.TipologiaFattura))
            conditions.Add("TipologiaFattura = @tipologiafattura");

        if (!string.IsNullOrEmpty(_command.Azione))
            conditions.Add("Azione = @azione");

        var where = conditions.Count > 0
            ? " WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

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
            TipologiaFattura = _command.TipologiaFattura,
            Azione = _command.Azione
        };


        using (var values = await ((IDatabase)this).QueryMultipleAsync<SimpleGestioneFattureDto>(
            connection!,
            sql,
            query,
            transaction,
            CommandType.Text,
            null,
            CommandFlags.NoCache))
        {
            Staginglist.GestioneFatture = await values.ReadAsync<SimpleGestioneFattureDto>();
            Staginglist.Count = await values.ReadFirstAsync<int>();
        }

        return Staginglist;

    }
}


