using System.Data;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Queries.Persistence;

public class MessaggiQueryGetByIdUtentePersistence(MessaggiQueryGetByIdUtente command) : DapperBase, IQuery<MessaggioListaDto?>
{
    private readonly MessaggiQueryGetByIdUtente _command = command;
    private static readonly string _sqlSelect = MessaggioSQLBuilder.Select();
    private static readonly string _sqlSelectAllCount = MessaggioSQLBuilder.SelectAllCount();
    private static readonly string _offSet = MessaggioSQLBuilder.OffSet();
    private static readonly string _orderBy = MessaggioSQLBuilder.OrderBy();
    public async Task<MessaggioListaDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var messaggi = new MessaggioListaDto();
        var idUtente = _command.AuthenticationInfo.Id;
        var page = _command.Page;
        var size = _command.Size;
        var anno = _command.AnnoValidita;
        var mese = _command.MeseValidita;
        var tipologiaDocumento = _command.TipologiaDocumento;
        var letto = _command.Letto;

        var where = " WHERE IdUtente=@IdUtente ";

        if (anno.HasValue)
            where += " AND Year(m.DataInserimento)=@Anno ";

        if (mese.HasValue)
            where += " AND Month(m.DataInserimento)=@Mese ";

        if (!_command.TipologiaDocumento!.IsNullNotAny())
            where += " AND TipologiaDocumento IN @TipologiaDocumento ";

        if (letto.HasValue)
            where += " AND Lettura=@letto ";

        var orderBy = _orderBy;

        var sql = _sqlSelect;
        var sqlCount = _sqlSelectAllCount;
        if (page == null && size == null)
            sql += where + orderBy;
        else
            sql += where + orderBy + _offSet;

        sqlCount += where;

        var sqlMessaggi = string.Join(";", sql, sqlCount);

        var query = new
        {
            Size = size,
            Page = page,
            IdUtente = idUtente,
            Anno = anno,
            Mese = mese,
            TipologiaDocumento = tipologiaDocumento,
            Letto = letto
        };

        using var values = await ((IDatabase)this).QueryMultipleAsync<MessaggioListaDto>(
            connection!,
            sqlMessaggi,
            query,
            transaction);
        messaggi.Messaggi = await values.ReadAsync<MessaggioDto>();
        messaggi.Count = await values.ReadFirstAsync<int>();

        return messaggi;
    }
}