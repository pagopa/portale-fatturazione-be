using System.Data;
using Dapper;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;
using static Dapper.SqlMapper;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureModuloCommessaExcelPersistence(FattureCommessaExcelQuery command) : DapperBase, IQuery<IEnumerable<FattureCommessaExcelDto>?>
{
    private readonly FattureCommessaExcelQuery _command = command;
    private static readonly string _sqlSelect = FattureModuloCommessaExcelBuilder.SelectCommesse();
    private static readonly string _sqlOrderby = FattureModuloCommessaExcelBuilder.OrderBy();
    public async Task<IEnumerable<FattureCommessaExcelDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var anno = _command.Anno;
        var mese = _command.Mese;
        var fatturaInviata = _command.FatturaInviata;
        var query = new DynamicParameters();
        query.Add("anno", anno);
        query.Add("mese", mese);
        query.Add("FatturaInviata", fatturaInviata.HasValue? fatturaInviata.Value: null);

        string where = string.Empty;
        if (!_command.IdEnti!.IsNullNotAny())
        {
            query.Add("IdEnti", _command.IdEnti);
            where += " AND t.FKIdEnte in @IdEnti ";
        }
         

        if(_command.FkIdTipoContratto.HasValue)
        {
            query.Add("FkIdTipoContratto", _command.FkIdTipoContratto, DbType.Int32);
            where += " AND c.FkIdTipoContratto = @FkIdTipoContratto ";
        } 

        var sql = _sqlSelect + where + _sqlOrderby; 

        return await ((IDatabase)this).SelectAsync<FattureCommessaExcelDto>(
         connection!,
         sql,
         query,
         transaction);
    }
}