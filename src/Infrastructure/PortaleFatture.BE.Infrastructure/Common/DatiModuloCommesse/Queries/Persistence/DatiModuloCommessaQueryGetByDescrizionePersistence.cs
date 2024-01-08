using System.Data;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaQueryGetByDescrizionePersistence(string? descrizione, int annoValidita, int meseValidita, string? prodotto) : DapperBase, IQuery<IEnumerable<ModuloCommessaByRicercaDto>?>
{
    private readonly string? _prodotto = prodotto;
    private readonly int _annoValidita = annoValidita;
    private readonly long _meseValidita = meseValidita;
    private readonly string? _descrizione = descrizione;
    private static readonly string _sqlSelect = DatiModuloCommessaSQLBuilder.SelectByRicerca();

    public async Task<IEnumerable<ModuloCommessaByRicercaDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        string? where = null;
        if (!string.IsNullOrEmpty(_descrizione))
            where = " AND d.description LIKE '%' + @descrizione + '%'";

        if (!string.IsNullOrEmpty(_prodotto))
            where += " AND d.FkProdotto  = @prodotto";

        return await ((IDatabase)this).SelectAsync<ModuloCommessaByRicercaDto>(connection!, _sqlSelect + where,
            new
            {
                descrizione = _descrizione,
                mese = _meseValidita,
                anno = _annoValidita,
                prodotto = _prodotto,
            }, transaction);
    }
}