using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;

public class ContestazioniEntiQueryPersistence(ContestazioniEntiQuery command) : DapperBase, IQuery<IEnumerable<ContestazioneEnte>?>
{
    private readonly ContestazioniEntiQuery _command = command;
    private static readonly string _sql = ContestazioniMassiveSQLBuilder.SelectEnti(); 
    public async Task<IEnumerable<ContestazioneEnte>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        dynamic parameters = new ExpandoObject();
        var where = string.Empty;
        if (!string.IsNullOrEmpty(_command.Descrizione))
        {
            where = " WHERE description like '%' + @Descrizione + '%'";
            parameters.Descrizione = _command.Descrizione;
        }
        return await ((IDatabase)this).SelectAsync<ContestazioneEnte>(
            connection!, _sql + where, parameters, transaction); 
    }
}