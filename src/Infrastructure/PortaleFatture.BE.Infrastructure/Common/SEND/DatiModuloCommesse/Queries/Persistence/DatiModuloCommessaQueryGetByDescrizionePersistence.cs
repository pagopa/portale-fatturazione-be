using System.Data;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaQueryGetByDescrizionePersistence(string[]? idEnti, int annoValidita, int meseValidita, string? prodotto) : DapperBase, IQuery<IEnumerable<ModuloCommessaByRicercaDto>?>
{
    private readonly string? _prodotto = prodotto;
    private readonly int _annoValidita = annoValidita;
    private readonly long _meseValidita = meseValidita;
    private readonly string[]? _idEnti = idEnti;
    private static readonly string _sqlSelect = DatiModuloCommessaSQLBuilder.SelectByRicerca();

    public async Task<IEnumerable<ModuloCommessaByRicercaDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        string? where = null;
        if (!_idEnti!.IsNullNotAny())
            where = " AND IdEnte in @idEnti";

        if (!string.IsNullOrEmpty(_prodotto))
            where += " AND Prodotto  = @prodotto";
        return await ((IDatabase)this).SelectAsync<ModuloCommessaByRicercaDto>(connection!, _sqlSelect.Add(schema) + where,
            new
            {
                idEnti = _idEnti,
                mese = _meseValidita,
                anno = _annoValidita,
                prodotto = _prodotto,
            }, transaction);
    }
}