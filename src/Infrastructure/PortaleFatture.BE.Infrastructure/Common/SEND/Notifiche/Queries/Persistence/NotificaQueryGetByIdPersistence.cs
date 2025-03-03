﻿using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

public class NotificaQueryGetByIdPersistence(NotificaQueryGetById command) : DapperBase, IQuery<Notifica?>
{
    private readonly NotificaQueryGetById _command = command;
    private static readonly string _sqlAll = NotificaSQLBuilder.SelectAll();
    public async Task<Notifica?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var sql = _sqlAll + " WHERE n.event_id=@IdNotifica ";
        try
        {
            var notifica = await ((IDatabase)this).SingleAsync<Notifica>(
                connection!, sql.Add(schema), _command, transaction);

            return notifica;
        }
        catch
        {
            return null;
        }
    }
}