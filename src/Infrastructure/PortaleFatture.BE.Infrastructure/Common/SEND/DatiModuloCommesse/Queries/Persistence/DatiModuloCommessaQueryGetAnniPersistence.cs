using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaQueryGetAnniPersistence(string? idEnte, string? prodotto) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly string? _idEnte = idEnte;
    private readonly string? _prodotto = prodotto;
    private static readonly string _sqlSelect = DatiModuloCommessaAnniSQLBuilder.SelectBy();

    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        return await ((IDatabase)this).SelectAsync<string>(connection!, _sqlSelect.Add(schema),
            new
            {
                idEnte = _idEnte,
                prodotto = _prodotto
            }, transaction);
    }
}