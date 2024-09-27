using System.Data;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

public class SpedizioneQueryGetAllPersistence : DapperBase, IQuery<IEnumerable<CategoriaSpedizione>?>
{
    private static readonly string _sqlSelect = string.Join(";", CategoriaSpedizioneSQLBuilder.SelectAll(), TipoSpedizioneSQLBuilder.SelectAll());
    public async Task<IEnumerable<CategoriaSpedizione>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        using var categorie = await ((IDatabase)this).QueryMultipleAsync<CategoriaSpedizione>(connection!, _sqlSelect.Add(schema), null, transaction);
        if (categorie == null)
            return null;
        var masterCategorie = await categorie.ReadAsync<CategoriaSpedizione>();
        var tipi = await categorie.ReadAsync<TipoSpedizione>();
        foreach (var cat in masterCategorie)
            cat.TipoSpedizione = tipi.Where(x => x.IdCategoriaSpedizione == cat.Id).ToList();

        return masterCategorie;
    }
}