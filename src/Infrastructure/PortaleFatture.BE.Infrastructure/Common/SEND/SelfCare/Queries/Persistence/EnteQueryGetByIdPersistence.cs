﻿using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence;

public class EnteQueryGetByIdPersistence : DapperBase, IQuery<Ente?>
{
    private readonly string? _idEnte;
    private static readonly string _sqlSelect = EnteSQLBuilder.SelectByIdEnte();

    public EnteQueryGetByIdPersistence(string idEnte)
    {
        _idEnte = idEnte;
    }
    public async Task<Ente?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var results = await ((IDatabase)this).SelectAsync<Ente>(connection!, _sqlSelect.Add(schema), new { IdEnte = _idEnte }, transaction);
        return results?.FirstOrDefault();
    }
}