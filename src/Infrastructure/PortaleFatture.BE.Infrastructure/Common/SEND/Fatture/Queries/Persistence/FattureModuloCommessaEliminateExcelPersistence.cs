using System.Data;
using Dapper;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureModuloCommessaEliminateExcelPersistence(FattureCommessaEliminateExcelQuery command) : DapperBase, IQuery<IEnumerable<FattureCommessaExcelDto>?>
{
    private readonly FattureCommessaEliminateExcelQuery _command = command;
    private static readonly string _sqlSelect = FattureModuloCommessaExcelBuilder.SelectCommesseEliminate();
    private static readonly string _sqlOrderby = FattureModuloCommessaExcelBuilder.OrderBy();
    public async Task<IEnumerable<FattureCommessaExcelDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        var anno = _command.Anno;
        var mese = _command.Mese;
        var query = new DynamicParameters();
        query.Add("anno", anno);
        query.Add("mese", mese);


        string where = string.Empty;  

        if (!_command.IdEnti!.IsNullNotAny())
        {
            query.Add("IdEnti", _command.IdEnti);
            where += " AND t.FKIdEnte in @IdEnti ";
        } 

        if (_command.FkIdTipoContratto.HasValue)
        {
            query.Add("FkIdTipoContratto", _command.FkIdTipoContratto, DbType.Int32);
            where += " AND c.FkIdTipoContratto = @FkIdTipoContratto ";
        }

        return await ((IDatabase)this).SelectAsync<FattureCommessaExcelDto>(
         connection!,
         _sqlSelect + where + _sqlOrderby,
         query,
         transaction);
    }
}