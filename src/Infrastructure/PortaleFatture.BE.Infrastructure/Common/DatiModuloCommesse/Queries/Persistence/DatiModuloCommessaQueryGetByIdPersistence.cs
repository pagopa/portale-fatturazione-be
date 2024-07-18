using System.Data;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaQueryGetByIdPersistence(string? idEnte, int annoValidita, int meseValidita, string? prodotto) : DapperBase, IQuery<IEnumerable<DatiModuloCommessa>?>
{
    private readonly string? _prodotto = prodotto; 
    private readonly int _annoValidita = annoValidita;
    private readonly long _meseValidita = meseValidita;
    private readonly string? _idEnte = idEnte;
    private static readonly string _sqlSelect = DatiModuloCommessaSQLBuilder.SelectBy();

    public async Task<IEnumerable<DatiModuloCommessa>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        return await ((IDatabase)this).SelectAsync<DatiModuloCommessa>(connection!, _sqlSelect.Add(schema),
            new
            {
                idEnte = _idEnte,
                meseValidita = _meseValidita,
                annoValidita = _annoValidita,
                prodotto = _prodotto,
            }, transaction); 
    }
}