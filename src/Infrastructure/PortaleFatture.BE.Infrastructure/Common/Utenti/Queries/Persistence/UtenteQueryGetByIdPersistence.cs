using System.Data;
using PortaleFatture.BE.Core.Entities.Utenti;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Utenti.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.Utenti.Queries.Persistence;

public class UtenteQueryGetByIdPersistence : DapperBase, IQuery<Utente?>
{
    private readonly string? _idEnte;
    private readonly string? _idUtente;
    private static readonly string _sqlSelect = UtenteSQLBuilder.SelectBy();

    public UtenteQueryGetByIdPersistence(string idEnte, string idUtente)
    {
        this._idEnte = idEnte;
        this._idUtente = idUtente;
    }
    public async Task<Utente?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var results = await ((IDatabase)this).SelectAsync<Utente>(connection!, _sqlSelect.Add(schema), new { IdEnte = _idEnte, IdUtente = _idUtente }, transaction);
        return results?.FirstOrDefault();
    }
}