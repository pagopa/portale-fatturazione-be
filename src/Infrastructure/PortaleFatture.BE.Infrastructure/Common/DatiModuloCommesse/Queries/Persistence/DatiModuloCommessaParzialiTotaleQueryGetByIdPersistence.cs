using System.Data;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaParzialiTotaleQueryGetByIdPersistence : DapperBase, IQuery<IEnumerable<DatiModuloCommessaParzialiTotale>?>
{
    private readonly string? _prodotto;
    private readonly long? _idTipoContratto;
    private readonly int _annoValidita;
    private readonly string? _idEnte;
    private readonly string? _ruolo;
    private static readonly string _sqlSelect = String.Join(";", DatiModuloCommessaTotaleSQLBuilder.SelectByAnno(), DatiModuloCommessaSQLBuilder.SelectByAnno());

    public DatiModuloCommessaParzialiTotaleQueryGetByIdPersistence(string? idEnte, int annoValidita, string? prodotto, string? ruolo)
    {
        this._prodotto = prodotto;
        this._annoValidita = annoValidita;
        this._idEnte = idEnte;
        this._ruolo = ruolo;
    }
    public async Task<IEnumerable<DatiModuloCommessaParzialiTotale>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        var parziali = new Dictionary<int, DatiModuloCommessaParzialiTotale>();
        using var values = await ((IDatabase)this).QueryMultipleAsync<DatiModuloCommessaTotale>(connection!, _sqlSelect.Add(schema),
            new
            {
                idEnte = _idEnte,
                annoValidita = _annoValidita,
                prodotto = _prodotto,
            }, transaction);

        var totalePerCategoria = await values.ReadAsync<DatiModuloCommessaTotale>();
        var moduliCommessa = await values.ReadAsync<DatiModuloCommessa>();
        foreach (var tot in totalePerCategoria)
        {
            parziali.TryGetValue(tot.MeseValidita, out DatiModuloCommessaParzialiTotale? parziale);
            parziale ??= new();

            parziale.Totale += tot.Totale;
            parziale.AnnoValidita = tot.AnnoValidita;
            parziale.MeseValidita = tot.MeseValidita;
            parziale.Prodotto = tot.Prodotto;
            parziale.IdEnte = tot.IdEnte;
            parziale.IdTipoContratto = tot.IdTipoContratto;
            parziale.Stato = tot.Stato;
            parziali.TryAdd(tot.MeseValidita, parziale);
            parziale.Modifica = tot.Stato == StatoModuloCommessa.ApertaCaricato && _ruolo == Ruolo.ADMIN;
        }
        foreach (var commessa in moduliCommessa)
        {
            parziali.TryGetValue(commessa.MeseValidita, out DatiModuloCommessaParzialiTotale? parziale);
            parziale ??= new();

            if (commessa.IdTipoSpedizione == 1) // Analog.A/R
            {
                parziale.AnalogicoARNazionali += commessa.ValoreNazionali;
                parziale.AnalogicoARInternazionali += commessa.ValoreInternazionali;
            }
            else if (commessa.IdTipoSpedizione == 2) // Analog.L. 890/82
                parziale.Analogico890Nazionali += commessa.ValoreNazionali;
            else //	Digitale
                parziale.Digitale = totalePerCategoria.Where(x => x.IdCategoriaSpedizione == 2).Select(x => x.Totale).FirstOrDefault();

        }
        return parziali.Values.ToList();
    }
}