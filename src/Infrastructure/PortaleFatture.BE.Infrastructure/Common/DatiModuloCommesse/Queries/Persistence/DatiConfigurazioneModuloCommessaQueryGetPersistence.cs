using System.Data;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence;

public class DatiConfigurazioneModuloCommessaQueryGetPersistence : DapperBase, IQuery<DatiConfigurazioneModuloCommessa?>
{
    private readonly string? _prodotto;
    private readonly long _idTipoContratto;
    private static readonly string _sqlSelect = String.Join(";", DatiConfigurazioneModuloCommessaSQLBuilder.SelectTipoBy(), DatiConfigurazioneModuloCommessaSQLBuilder.SelectCategoriaBy());
    public DatiConfigurazioneModuloCommessaQueryGetPersistence(long idTipoContratto, string? prodotto)
    {
        this._idTipoContratto = idTipoContratto;
        this._prodotto = prodotto;
    }

    public async Task<DatiConfigurazioneModuloCommessa?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        var values = await ((IDatabase)this).QueryMultipleAsync<DatiConfigurazioneModuloTipoCommessa>(connection!, _sqlSelect.Add(schema),
            new
            {
                prodotto = _prodotto,
                tipoContratto = _idTipoContratto,
                idTipoContratto = _idTipoContratto
            }, transaction);

        if (values == null)
            return null;
        var tipi = await values.ReadAsync<DatiConfigurazioneModuloTipoCommessa>();
        var categorie = await values.ReadAsync<DatiConfigurazioneModuloCategoriaCommessa>();
        if (tipi.Any() && categorie.Any())
            return new DatiConfigurazioneModuloCommessa()
            {
                Tipi = tipi,
                Categorie = categorie
            };
        return null;
    }
}