using System.Data;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

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

        var where = " WHERE t.AnnoValidita=@anno and t.MeseValidita=@mese ";

        if (!_command.IdEnti!.IsNullNotAny())
            where = where + " AND t.FKIdEnte in @IdEnti ";

        var sql = _sqlSelect + where + _sqlOrderby;

        var query = new
        {
            Anno = anno,
            Mese = mese,
            _command.IdEnti
        };

        return await ((IDatabase)this).SelectAsync<FattureCommessaExcelDto>(
         connection!,
         sql,
         query,
         transaction);
    }
}