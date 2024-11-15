﻿using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaQueryGetByAnnoPersistence(string? idEnte, int annoValidita, string? prodotto) : DapperBase, IQuery<IEnumerable<DatiModuloCommessaTotale>?>
{
    private readonly int _annoValidita = annoValidita;
    private readonly string? _idEnte = idEnte;
    private readonly string? _prodotto = prodotto;
    private static readonly string _sqlSelect = DatiModuloCommessaTotaleSQLBuilder.SelectByAnno();

    public async Task<IEnumerable<DatiModuloCommessaTotale>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        return await ((IDatabase)this).SelectAsync<DatiModuloCommessaTotale>(connection!, _sqlSelect.Add(schema),
             new
             {
                 idEnte = _idEnte,
                 annoValidita = _annoValidita,
                 prodotto = _prodotto,
             }, transaction);
    }
}