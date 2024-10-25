using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

public class DatiConfigurazioneModuloCommessaQueryGetPersistence : DapperBase, IQuery<DatiConfigurazioneModuloCommessa?>
{
    private readonly string? _prodotto;
    private readonly long _idTipoContratto;
    private static readonly string _sqlSelect = string.Join(";", DatiConfigurazioneModuloCommessaSQLBuilder.SelectTipoBy(), DatiConfigurazioneModuloCommessaSQLBuilder.SelectCategoriaBy());
    public DatiConfigurazioneModuloCommessaQueryGetPersistence(long idTipoContratto, string? prodotto)
    {
        _idTipoContratto = idTipoContratto;
        _prodotto = prodotto;
    }

    public async Task<DatiConfigurazioneModuloCommessa?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        using var values = await ((IDatabase)this).QueryMultipleAsync<DatiConfigurazioneModuloTipoCommessa>(connection!, _sqlSelect.Add(schema),
            new
            {
                prodotto = _prodotto,
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