using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureTipologiaAnniMeseQueryPersistence(FattureTipologiaAnniMeseQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly FattureTipologiaAnniMeseQuery _command = command;
    private static readonly string _sql = FattureQueryRicercaBuilder.SelectTipologiaFatturaAnnoMese(); 
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        dynamic parameters = new ExpandoObject(); 
        parameters.Anno = _command.Anno; 
        parameters.mese = _command.Mese;

        return await ((IDatabase)this).SelectAsync<string>(
            connection!, _sql, parameters, transaction);
    }
}