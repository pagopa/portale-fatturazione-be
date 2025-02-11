using System.Data;
using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.Tipologie;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class RicercaContrattiTipologiaPersistence(RicercaContrattiTipologiaQuery command) : DapperBase, IQuery<ContrattiTipologiaDto?>
{
    private readonly RicercaContrattiTipologiaQuery _command = command;
    private static readonly string _sqlSelectAll = ContrattiTipologiaSQLBuilder.SelectAll();
    private static readonly string _sqlSelectAllCount = ContrattiTipologiaSQLBuilder.SelectAllCount();
    private static readonly string _offSet = ContrattiTipologiaSQLBuilder.OffSet();
    private static readonly string _orderBy = ContrattiTipologiaSQLBuilder.OrderBy();

    public async Task<ContrattiTipologiaDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var listaContratti = new ContrattiTipologiaDto();
        var where = string.Empty;
 
        var page = _command.Page;
        var size = _command.Size;
        where += " WHERE DataCancellazione is null";

        if (!_command.IdEnti!.IsNullNotAny())
            where += $" AND e.InternalIstitutionId IN @identi";

        if (_command.TipologiaContratto.HasValue)
            where += $" AND c.FkIdTipoContratto = @tipocontratto";

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
            Tipocontratto = _command.TipologiaContratto
        }; 
      

        using (var values = await ((IDatabase)this).QueryMultipleAsync<SingleContrattiTipologiaDto>(
            connection!,
            sql,
            query,
            transaction,
            CommandType.Text,
            null,
            CommandFlags.NoCache))
        {
            listaContratti.Contratti = await values.ReadAsync<SingleContrattiTipologiaDto>();
            listaContratti.Count = await values.ReadFirstAsync<int>();
        }

        return listaContratti;
    }
}