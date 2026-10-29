using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

/// <summary>Stato corrente di una fattura, come letto dalla vista al momento della richiesta.</summary>
public sealed record InvioSapFatturaCorrente(
    long IdFattura,
    int Anno,
    int Mese,
    string TipologiaFattura,
    string StatoInvio);

/// <summary>Richiesta di generazione JSON (body di <c>PUT api/fatture/invio-sap/genera</c>), gia' deserializzata.</summary>
public sealed class InvioSapRichiestaGenerazione
{
    public int? Anno { get; init; }
    public int? Mese { get; init; }
    public string? TipologiaFattura { get; init; }
    public IReadOnlyList<long>? IdFatture { get; init; }
    public string? Nota { get; init; }
    public string? NumeroDocumentoStorno { get; init; }
}

/// <summary>Esito della validazione, nell'ordine in cui l'endpoint lo traduce in HTTP.</summary>
public enum InvioSapEsito
{
    /// <summary>202: si puo' avviare il batch.</summary>
    Ok,
    /// <summary>400: richiesta malformata o incompleta (anche storno mancante su fatture gia' inviate).</summary>
    NonValida,
    /// <summary>404: almeno una fattura selezionata non esiste nel perimetro.</summary>
    NonTrovate,
    /// <summary>409: almeno una fattura e' gia' in elaborazione (stato non piu' atteso).</summary>
    StatoNonAmmesso
}

public sealed class InvioSapValidazione
{
    public InvioSapEsito Esito { get; init; }
    public IReadOnlyList<string> Errori { get; init; } = [];
    /// <summary>Le fatture che hanno causato l'esito negativo (non trovate, fuori gruppo, in elaborazione).</summary>
    public IReadOnlyList<long> IdFattureCoinvolte { get; init; } = [];
    /// <summary>Vero se almeno una fattura selezionata e' gia' INVIATA: il batch e' un re-invio (RF-16).</summary>
    public bool RichiedeStorno { get; init; }
    public bool Valida => Esito == InvioSapEsito.Ok;
}

/// <summary>
/// Regole pure (nessun DB) del flusso "Genera JSON" di PF-735 (RF-6, RF-8, RF-10, RF-16; RI-7, RI-16).
/// Le usa il command handler prima di chiamare la stored procedure di avvio batch, che ripete i soli
/// controlli di stato in transazione (la verita' sullo stato e' nel DB, qui si evita un giro inutile).
///
/// Precedenza degli esiti, dalla piu' "colpa del client" alla piu' "colpa del momento":
/// formale (400) → non trovate (404) → fuori gruppo (400) → in elaborazione (409) → storno mancante (400).
/// </summary>
public static class InvioSapRegole
{
    public const int AnnoMinimo = 2000;
    public const string NomeFilePrefisso = "sap";

    // Messaggi: li legge l'admin nel 400, quindi in italiano e senza dettagli tecnici.
    public const string ErroreAnno = "Anno obbligatorio";
    public const string ErroreMese = "Mese obbligatorio, da 1 a 12";
    public const string ErroreTipologia = "Tipologia fattura obbligatoria";
    public const string ErroreSelezioneVuota = "Selezionare almeno una fattura";
    public const string ErroreSelezioneDuplicata = "La selezione contiene fatture ripetute";
    public const string ErroreIdNonValido = "Identificativo fattura non valido";
    public const string ErroreNota = "La nota e' obbligatoria";
    public const string ErroreStorno = "Numero documento di storno obbligatorio: la selezione contiene fatture gia' inviate";
    public const string ErroreNonTrovate = "Una o piu' fatture selezionate non esistono";
    public const string ErroreFuoriGruppo = "Tutte le fatture devono appartenere allo stesso anno, mese e tipologia";
    public const string ErroreInElaborazione = "Una o piu' fatture selezionate sono gia' in elaborazione";
    public const string ErroreStatoSconosciuto = "Stato di invio non riconosciuto";

    public static InvioSapValidazione ValidaGenerazione(
        InvioSapRichiestaGenerazione richiesta,
        IReadOnlyCollection<InvioSapFatturaCorrente>? fattureCorrenti)
    {
        ArgumentNullException.ThrowIfNull(richiesta);
        var errori = new List<string>();

        // 1. Formale: tutto cio' che si vede dal solo body.
        if (richiesta.Anno is null || richiesta.Anno < AnnoMinimo) errori.Add(ErroreAnno);
        if (richiesta.Mese is null || richiesta.Mese is < 1 or > 12) errori.Add(ErroreMese);
        if (string.IsNullOrWhiteSpace(richiesta.TipologiaFattura)) errori.Add(ErroreTipologia);

        var ids = richiesta.IdFatture ?? [];
        if (ids.Count == 0) errori.Add(ErroreSelezioneVuota);
        else
        {
            if (ids.Any(id => id <= 0)) errori.Add(ErroreIdNonValido);
            if (ids.Distinct().Count() != ids.Count) errori.Add(ErroreSelezioneDuplicata);
        }

        if (string.IsNullOrWhiteSpace(richiesta.Nota)) errori.Add(ErroreNota);

        if (errori.Count > 0)
            return new InvioSapValidazione { Esito = InvioSapEsito.NonValida, Errori = errori };

        // 2. Non trovate (404): id che la vista non restituisce (inesistenti, o esclusi dal perimetro:
        //    ente PagoPA, posticipate, eliminate — per il client e' lo stesso: non ci sono).
        var correnti = (fattureCorrenti ?? []).ToDictionary(f => f.IdFattura);
        var nonTrovate = ids.Where(id => !correnti.ContainsKey(id)).ToList();
        if (nonTrovate.Count > 0)
            return new InvioSapValidazione
            {
                Esito = InvioSapEsito.NonTrovate,
                Errori = [ErroreNonTrovate],
                IdFattureCoinvolte = nonTrovate
            };

        var selezionate = ids.Select(id => correnti[id]).ToList();

        // 3. Fuori gruppo (400): la selezione e' confinata al rigo espanso (RF-6).
        var tipologia = richiesta.TipologiaFattura!.Trim();
        var fuoriGruppo = selezionate
            .Where(f => f.Anno != richiesta.Anno
                     || f.Mese != richiesta.Mese
                     || !string.Equals(f.TipologiaFattura?.Trim(), tipologia, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.IdFattura)
            .ToList();
        if (fuoriGruppo.Count > 0)
            return new InvioSapValidazione
            {
                Esito = InvioSapEsito.NonValida,
                Errori = [ErroreFuoriGruppo],
                IdFattureCoinvolte = fuoriGruppo
            };

        // 4. Stato (409): nessuna fattura in elaborazione, nessuno stato sconosciuto.
        var inElaborazione = selezionate
            .Where(f => f.StatoInvio == StatoInvioSap.IN_ELABORAZIONE)
            .Select(f => f.IdFattura)
            .ToList();
        if (inElaborazione.Count > 0)
            return new InvioSapValidazione
            {
                Esito = InvioSapEsito.StatoNonAmmesso,
                Errori = [ErroreInElaborazione],
                IdFattureCoinvolte = inElaborazione
            };

        var sconosciute = selezionate
            .Where(f => !StatoInvioSap.IsValido(f.StatoInvio))
            .Select(f => f.IdFattura)
            .ToList();
        if (sconosciute.Count > 0)
            return new InvioSapValidazione
            {
                Esito = InvioSapEsito.StatoNonAmmesso,
                Errori = [ErroreStatoSconosciuto],
                IdFattureCoinvolte = sconosciute
            };

        // 5. Storno (400): obbligatorio se almeno una selezionata e' gia' INVIATA (RF-16).
        var richiedeStorno = selezionate.Any(f => f.StatoInvio == StatoInvioSap.INVIATA);
        if (richiedeStorno && string.IsNullOrWhiteSpace(richiesta.NumeroDocumentoStorno))
            return new InvioSapValidazione
            {
                Esito = InvioSapEsito.NonValida,
                Errori = [ErroreStorno],
                RichiedeStorno = true,
                IdFattureCoinvolte = selezionate
                    .Where(f => f.StatoInvio == StatoInvioSap.INVIATA)
                    .Select(f => f.IdFattura)
                    .ToList()
            };

        return new InvioSapValidazione { Esito = InvioSapEsito.Ok, RichiedeStorno = richiedeStorno };
    }

    /// <summary>
    /// Filtri della griglia (RF-15: anno obbligatorio e singolo). <paramref name="annoCorrente"/> e'
    /// passato dal chiamante per rendere la regola testabile senza dipendere dall'orologio.
    /// </summary>
    public static IReadOnlyList<string> ValidaFiltriGriglia(
        int? anno,
        IEnumerable<int>? mesi,
        IEnumerable<string?>? tipologieFattura,
        string? statoInvio,
        int annoCorrente)
    {
        var errori = new List<string>();

        if (anno is null) errori.Add(ErroreAnno);
        else if (anno < AnnoMinimo || anno > annoCorrente + 1) errori.Add($"Anno fuori intervallo ({AnnoMinimo}-{annoCorrente + 1})");

        var listaMesi = mesi?.ToList() ?? [];
        if (listaMesi.Any(m => m is < 1 or > 12)) errori.Add(ErroreMese);
        if (listaMesi.Distinct().Count() != listaMesi.Count) errori.Add("Mesi ripetuti nel filtro");

        var listaTipologie = tipologieFattura?.ToList() ?? [];
        if (listaTipologie.Any(string.IsNullOrWhiteSpace)) errori.Add(ErroreTipologia);

        if (!FiltroStatoInvioSap.IsValido(statoInvio))
            errori.Add($"Stato invio non valido: ammessi {string.Join(", ", FiltroStatoInvioSap.Tutti)}");

        return errori;
    }

    /// <summary>
    /// <c>sap-yyyyMMdd-HHmmss-&lt;batchId&gt;.json</c>, data in orario italiano (RI-9). Lo genera il
    /// backend e lo scrive nel Registro prima di avviare la pipeline: DATA deve usare esattamente questo nome.
    /// </summary>
    public static string NomeFileJson(DateTime utcNow, Guid batchId)
    {
        var locale = utcNow.ItalianTime();
        return $"{NomeFilePrefisso}-{locale:yyyyMMdd}-{locale:HHmmss}-{batchId:D}.json";
    }
}
