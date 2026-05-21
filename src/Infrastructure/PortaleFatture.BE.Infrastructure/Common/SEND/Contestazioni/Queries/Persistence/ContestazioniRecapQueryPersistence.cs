using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;

public class ContestazioniRecapQueryPersistence(ContestazioniRecapQuery command) : DapperBase, IQuery<IEnumerable<ContestazioneRecapDto>?>
{
    private readonly ContestazioniRecapQuery _command = command;
    private static readonly string _sql = ContestazioniMassiveSQLBuilder.SelectRecap();

    public async Task<IEnumerable<ContestazioneRecapDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        dynamic parameters = new ExpandoObject();

        var where = "WHERE 1=1"; //per evitare di dover gestire la logica di concatenazione degli AND

        if (!string.IsNullOrEmpty(_command.Anno))
        {
            where += " AND [Anno]=@anno ";
            parameters.Anno = _command.Anno;
        }
        if (!string.IsNullOrEmpty(_command.Mese))
        {
            where += "  AND [Mese]=@mese ";
            parameters.Mese = _command.Mese;
        }

        if (!string.IsNullOrEmpty(_command.IdEnte))
        {
            where += " AND [IdEnte]=@idEnte ";
            parameters.IdEnte = _command.IdEnte;
        }

        return await ((IDatabase)this).SelectAsync<ContestazioneRecapDto>(
            connection!, _sql + where, _command, transaction); 
    }
}