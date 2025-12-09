using System.Data;
using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Dto;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;
 

public class RicercaModuloCommessaByDateQueryPersistence(RicercaModuloCommessaByDateQuery? command) : DapperBase, IQuery<ModuloCommessaPrevisionaleDateDto>
{ 
    private static readonly string _sqlSelect = DatiModuloCommessaDateSQLBuilder.GetSelectDate();
    private static readonly string _sqlSelectCount = DatiModuloCommessaDateSQLBuilder.GetSelectCountDate();
    public async Task<ModuloCommessaPrevisionaleDateDto> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        var model = new ModuloCommessaPrevisionaleDateDto();
        var sql = _sqlSelect;
        var sqlCount = _sqlSelectCount;
 
        if (command!.IdEnti!.IsNullNotAny())
        {
            sql = sql.Replace("[whereIdenti]", string.Empty);
            sqlCount = sqlCount.Replace("[whereIdenti]", string.Empty);
        } 
        else
        {
            sql = sql.Replace("[whereIdenti]", " WHERE t.FkIdEnte IN @identi ");
            sqlCount = sqlCount.Replace("[whereIdenti]", " WHERE t.FkIdEnte IN @identi ");
        }
           

        var sqlMultiple = String.Join(";", sql, sqlCount);
        using var values = await ((IDatabase)this).QueryMultipleAsync<PSP>(
            connection!,
            sqlMultiple,
            command,
            transaction,
            CommandType.Text,
            null,
            CommandFlags.NoCache);
        model.ModuliCommessa = (IList<ModuloCommessaPrevisionaleByAnnoDto>?)await values.ReadAsync<ModuloCommessaPrevisionaleByAnnoDto>();
        model.Count = await values.ReadFirstAsync<int>();

        return model;
    }
}