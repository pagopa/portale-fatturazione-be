using System.Data;
using DocumentFormat.OpenXml.InkML;
using PortaleFatture.BE.Core.Entities.Scadenziari;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Scadenziari.Queries.Persistence;

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