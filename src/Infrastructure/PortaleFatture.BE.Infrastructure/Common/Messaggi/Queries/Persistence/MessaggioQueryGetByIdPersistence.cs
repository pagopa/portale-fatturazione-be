using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Messaggi.Dto;
using PortaleFatture.BE.Infrastructure.Common.Messaggi.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Messaggi.Queries.Persistence;

public class MessaggioQueryGetByIdPersistence(MessaggioQueryGetById command) : DapperBase, IQuery<MessaggioDto?>
{
    private readonly MessaggioQueryGetById _command = command;
    private static readonly string _sqlSelect = MessaggioSQLBuilder.Select();
    public async Task<MessaggioDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        MessaggioDto messaggio;
        var idUtente = _command.AuthenticationInfo.Id;
        var idMessaggio = _command.IdMessaggio;
        var where = " WHERE IdMessaggio=@IdMessaggio "; 
        var sql = _sqlSelect + where;

        try
        {
            messaggio = await ((IDatabase)this).SingleAsync<MessaggioDto>(
                    connection!,
                    sql,
                    new
                    {
                        IdMessaggio = idMessaggio
                    },
                    transaction); 
        }
        catch
        {
            return null;
        }

        if (messaggio != null && messaggio.IdUtente == idUtente)
            return messaggio;
        else
            return null;
    }
}