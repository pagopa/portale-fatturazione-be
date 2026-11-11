using System.Data;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries.Persistence;

/// <summary>
/// Legge i tipi ente distinti da [be].[vwAppioContractsFiltroTipoEnte].
/// </summary>
/// <param name="command">La query (senza filtri).</param>
public sealed class AppIoContrattiTipiEnteQueryPersistence(AppIoContrattiTipiEnteQuery command) : DapperBase, IQuery<IEnumerable<string>>
{
    private readonly AppIoContrattiTipiEnteQuery _command = command;
    private static readonly string _sql = AppIoContrattiSQLBuilder.SelectTipiEnte();

    /// <summary>
    /// Restituisce i tipi ente distinti, in ordine alfabetico.
    /// </summary>
    /// <param name="connection">La connessione al database.</param>
    /// <param name="schema">Lo schema del contesto (non usato: la vista è nello schema be).</param>
    /// <param name="transaction">La transazione corrente, se presente.</param>
    /// <param name="cancellationToken">Il token di annullamento.</param>
    /// <returns>I tipi ente.</returns>
    public async Task<IEnumerable<string>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<string>(
           connection!,
           _sql,
           new { },
           transaction);
    }
}
