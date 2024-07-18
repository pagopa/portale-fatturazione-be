using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Asseverazione.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;

public class AsseverazioneQueryGetPersistence : DapperBase, IQuery<IEnumerable<EnteAsserverazioneDto>>
{
    private readonly AsseverazioneQueryGet _command;
    private static readonly string _sqlSelect = AsseverazioneSQLBuilder.SelectAll();
    public AsseverazioneQueryGetPersistence(AsseverazioneQueryGet command)
    {
        this._command = command;
    }

    public async Task<IEnumerable<EnteAsserverazioneDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        return await ((IDatabase)this).SelectAsync<EnteAsserverazioneDto>(connection!, _sqlSelect.Add(schema), _command, transaction); 
    } 
} 