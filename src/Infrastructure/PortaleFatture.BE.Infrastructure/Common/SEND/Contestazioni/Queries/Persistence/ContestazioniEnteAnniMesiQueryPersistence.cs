using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;

public class ContestazioniEnteAnniMesiQueryPersistence(ContestazioniEnteAnniMesiQuery command) : DapperBase, IQuery<IEnumerable<ContestazioneEnteAnniMesi>?>
{
    private readonly ContestazioniEnteAnniMesiQuery _command = command;
    private static readonly string _sql = ContestazioniMassiveSQLBuilder.SelectContestazioneEnteAnniMesi();
    public async Task<IEnumerable<ContestazioneEnteAnniMesi>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {

        return await ((IDatabase)this).SelectAsync<ContestazioneEnteAnniMesi>(
            connection!, _sql, _command, transaction);
    }
}