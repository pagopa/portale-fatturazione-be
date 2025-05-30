﻿using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

public class ContestazioneQueryGetByIdNotificaPersistence(ContestazioneQueryGetByIdNotifica command) : DapperBase, IQuery<Contestazione?>
{
    private readonly ContestazioneQueryGetByIdNotifica _command = command;
    private static readonly string _sql = ContestazioneSQLBuilder.SelectByIdNotifica();
    public async Task<Contestazione?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            var contestazione = await ((IDatabase)this).SingleAsync<Contestazione>(
                connection!, _sql.Add(schema), _command, transaction);
            return contestazione;
        }
        catch
        {
            return null;
        }
    }
}