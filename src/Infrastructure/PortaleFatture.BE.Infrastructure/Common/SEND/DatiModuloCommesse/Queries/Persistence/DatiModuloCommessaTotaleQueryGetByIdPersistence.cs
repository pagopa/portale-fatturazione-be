using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaTotaleQueryGetByIdPersistence : DapperBase, IQuery<IEnumerable<DatiModuloCommessaTotale>?>
{
    private readonly string? _prodotto;
    private readonly int _annoValidita;
    private readonly long _meseValidita;
    private readonly string? _idEnte;
    private static readonly string _sqlSelect = DatiModuloCommessaTotaleSQLBuilder.SelectBy();

    public DatiModuloCommessaTotaleQueryGetByIdPersistence(string? idEnte, int annoValidita, int meseValidita, string? prodotto)
    {
        _prodotto = prodotto;
        _annoValidita = annoValidita;
        _meseValidita = meseValidita;
        _idEnte = idEnte;
    }
    public async Task<IEnumerable<DatiModuloCommessaTotale>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        return await ((IDatabase)this).SelectAsync<DatiModuloCommessaTotale>(connection!, _sqlSelect.Add(schema),
            new
            {
                idEnte = _idEnte,
                meseValidita = _meseValidita,
                annoValidita = _annoValidita,
                prodotto = _prodotto,
            }, transaction);
    }
}