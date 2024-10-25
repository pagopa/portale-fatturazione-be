using System.Data;
using DocumentFormat.OpenXml.InkML;
using PortaleFatture.BE.Core.Entities.SEND.Scadenziari;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries.Persistence;

public class CalendarioContestazioneQueryGetPersistence(CalendarioContestazioneQueryGet command) : DapperBase, IQuery<CalendarioContestazione?>
{
    private readonly CalendarioContestazioneQueryGet _command = command;
    private static readonly string _sql = CalendarioContestazioneSQLBuilder.SelectByAnnoMese();
    public async Task<CalendarioContestazione?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ((IDatabase)this).SingleAsync<CalendarioContestazione>(connection!, _sql.Add(schema), new
            {
                AnnoContestazione = _command.Anno,
                MeseContestazione = _command.Mese
            }, transaction);
        }
        catch
        {
            return null;
        }
    }
}