using System.Data;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaQueryGetAnniPersistence(string? idEnte) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly string? _idEnte = idEnte;
    private static readonly string _sqlSelect = DatiModuloCommessaAnniSQLBuilder.SelectBy();

    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        return await ((IDatabase)this).SelectAsync<string>(connection!, _sqlSelect.Add(schema), new { idEnte = _idEnte }, transaction);
    }
}