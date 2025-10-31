using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaAderentiQueryGetPersistence(string? idEnte) : DapperBase, IQuery<DatiModuloCommessaAderentiDto?>
{
    private readonly string? _IdEnte = idEnte;
    private static readonly string _sqlSelect = DatiModuloCommessaSQLBuilder.SelectAderenti();

    public async Task<DatiModuloCommessaAderentiDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        return await ((IDatabase)this).SingleAsync<DatiModuloCommessaAderentiDto>(connection!, _sqlSelect.Add(schema),
              new
              {
                  idEnte = _IdEnte
              }, transaction);
    }
}