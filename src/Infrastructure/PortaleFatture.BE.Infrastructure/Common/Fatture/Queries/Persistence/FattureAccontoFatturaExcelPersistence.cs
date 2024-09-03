using System.Data;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence;

public class FattureAccontoFatturaExcelPersistence(FattureAccontoExcelQuery command) : DapperBase, IQuery<IEnumerable<FattureAccontoExcelDto>?>
{
    private readonly FattureAccontoExcelQuery _command = command;
    private static readonly string _sql = FattureAccontoFatturaExcelBuilder.SelectAcconto(); 
    public async Task<IEnumerable<FattureAccontoExcelDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var anno = _command.Anno;
        var mese = _command.Mese;
        var where = " where n.year= @anno and n.month=@mese ";

        if (!_command.IdEnti!.IsNullNotAny())
            where = " AND t.FKIdEnte in @IdEnti ";

        var sql = _sql + where;

        var query = new
        {
            Anno = anno,
            Mese = mese, 
            IdEnti = _command.IdEnti
        };
 
        return await ((IDatabase)this).SelectAsync<FattureAccontoExcelDto>(
        connection!,
        sql,
        query,
        transaction); 
    }
}