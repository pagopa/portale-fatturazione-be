using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;

public class ContestazioniRecapQueryPersistence(ContestazioniRecapQuery command) : DapperBase, IQuery<IEnumerable<ContestazioneRecap>?>
{
    private readonly ContestazioniRecapQuery _command = command;
    private static readonly string _sql = ContestazioniMassiveSQLBuilder.SelectRecap();
    private static readonly string _gorderBy = ContestazioniMassiveSQLBuilder.GroupByOrderByRecap();
    public async Task<IEnumerable<ContestazioneRecap>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        dynamic parameters = new ExpandoObject();
        var where = string.Empty;
        if (!string.IsNullOrEmpty(_command.Anno))
        {
            where += " AND N.year=@anno ";
            parameters.Anno = _command.Anno;
        }
        if (!string.IsNullOrEmpty(_command.Mese))
        {
            where += "  AND N.month=@mese ";
            parameters.Anno = _command.Anno;
        }
        if (!string.IsNullOrEmpty(_command.ContractId))
        {
            where += " AND N.contract_id=@ContractId ";
            parameters.Anno = _command.Anno;
        }

        if (!string.IsNullOrEmpty(_command.IdEnte))
        {
            where += " AND N.internal_organization_id=@idEnte ";
            parameters.IdEnte = _command.IdEnte;
        }

        return await ((IDatabase)this).SelectAsync<ContestazioneRecap>(
            connection!, _sql + where + _gorderBy, _command, transaction); 
    }
}