namespace PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

/// <summary>Le due cose che si possono generare (D7 di be-invio-sap.md).</summary>
public enum InvioSapModalita
{
    /// <summary>Rigo <c>DA_INVIARE</c> della griglia, intero: nessuna selezione, nessuno storno.</summary>
    PrimoInvio,
    /// <summary>Fatture scelte nel dettaglio di un rigo <c>INVIATE</c>, con storno obbligatorio.</summary>
    ReInvio
}

/// <summary>Body di <c>PUT api/fatture/invio-sap/genera</c>, gia' deserializzato.</summary>
public sealed class InvioSapRichiestaGenerazione
{
    public int? Anno { get; init; }
    public int? Mese { get; init; }
    public string? TipologiaFattura { get; init; }
    /// <summary>Assente = primo invio del rigo; presente (anche vuota) = re-invio.</summary>
    public IReadOnlyList<long>? IdFatture { get; init; }
    public string? Nota { get; init; }
    public string? NumeroDocumentoStorno { get; init; }
}

/// <summary>Una fattura scelta per il re-invio, come la legge la vista dettaglio.</summary>
public sealed record InvioSapFatturaCorrente(
    long IdFattura,
    int Anno,
    int Mese,
    string TipologiaFattura,
    bool? FatturaInviata);

/// <summary>
/// Regole pure (nessun DB) del Genera e dei filtri. Precedenza degli esiti del Genera:
/// lock 409 (nell'handler) → formale 400 → non trovate 404 → fuori gruppo o stato 400.
/// Lo storno e' parte della validazione formale, perche' dipende solo dalla modalita'.
/// </summary>
public static class InvioSapRegole
{
    /// <summary><c>pfw.TipoAzione</c> "GeneraJsonFatture".</summary>
    public static readonly Guid IdAzioneGeneraJson = new("0B102032-99F2-4AF9-BC5E-EBE844171DCC");
    public const string DescrizioneAzione = "GeneraJsonFatture";

    public const int AnnoMinimo = 2000;
    public const int MaxFatture = 1000;
    public const int MaxNota = 2000;
    public const int MaxStorno = 255;
    public const int MaxIdUtente = 50;
    public const int MaxRagioneSociale = 255;
    public const int MaxNumeroFattura = 50;
    public const int MaxPageSize = 500;

    // Messaggi: li legge l'admin nel 400, quindi in italiano e senza dettagli tecnici.
    public const string ErroreAnno = "Anno obbligatorio";
    public const string ErroreMese = "Mese obbligatorio, da 1 a 12";
    public const string ErroreTipologia = "Tipologia fattura obbligatoria";
    public const string ErroreSelezioneVuota = "Selezionare almeno una fattura";
    public const string ErroreSelezioneTroppoGrande = "Si possono re-inviare al massimo 1000 fatture per volta";
    public const string ErroreSelezioneDuplicata = "La selezione contiene fatture ripetute";
    public const string ErroreIdNonValido = "Identificativo fattura non valido";
    public const string ErroreNota = "La nota e' obbligatoria";
    public const string ErroreNotaLunga = "La nota supera i 2000 caratteri";
    public const string ErroreStornoMancante = "Numero documento di storno obbligatorio per il re-invio";
    public const string ErroreStornoNonAmmesso = "Il numero documento di storno non e' ammesso nel primo invio";
    public const string ErroreStornoLungo = "Il numero documento di storno supera i 255 caratteri";
    public const string ErroreUtente = "Utente non valido";
    public const string ErroreNonTrovate = "Una o piu' fatture selezionate non esistono";
    public const string ErroreFuoriGruppo = "Tutte le fatture devono appartenere all'anno, al mese e alla tipologia indicati";
    public const string ErroreNonInviate = "Si possono re-inviare solo fatture gia' inviate";
    public const string ErroreStatoFiltro = "Stato invio non valido: ammessi TUTTE, DA_INVIARE, INVIATE, IN_ELABORAZIONE";
    public const string ErroreDettaglioDaInviare = "Il dettaglio non e' disponibile per le fatture da inviare: il rigo si genera per intero";
    public const string ErroreStatoDettaglio = "Stato del rigo obbligatorio: DA_INVIARE, INVIATE o IN_ELABORAZIONE";
    public const string ErrorePaginazione = "Paginazione non valida";
    public const string ErroreRagioneSociale = "Ragione sociale troppo lunga";
    public const string ErroreNumeroFattura = "Numero fattura troppo lungo";

    public static InvioSapModalita Modalita(InvioSapRichiestaGenerazione richiesta) =>
        richiesta.IdFatture is null ? InvioSapModalita.PrimoInvio : InvioSapModalita.ReInvio;

    /// <summary>Tutto cio' che si vede dal solo body (e dall'utente): 400.</summary>
    public static IReadOnlyList<string> ValidaFormale(InvioSapRichiestaGenerazione richiesta, string? idUtente)
    {
        ArgumentNullException.ThrowIfNull(richiesta);
        var errori = new List<string>();

        if (richiesta.Anno is null || richiesta.Anno < AnnoMinimo || richiesta.Anno > 9999) errori.Add(ErroreAnno);
        if (richiesta.Mese is null or < 1 or > 12) errori.Add(ErroreMese);
        if (string.IsNullOrWhiteSpace(richiesta.TipologiaFattura)) errori.Add(ErroreTipologia);

        if (string.IsNullOrWhiteSpace(richiesta.Nota)) errori.Add(ErroreNota);
        else if (richiesta.Nota.Length > MaxNota) errori.Add(ErroreNotaLunga);

        if (string.IsNullOrWhiteSpace(idUtente) || idUtente.Length > MaxIdUtente) errori.Add(ErroreUtente);

        var storno = richiesta.NumeroDocumentoStorno;
        if (Modalita(richiesta) == InvioSapModalita.PrimoInvio)
        {
            if (!string.IsNullOrEmpty(storno)) errori.Add(ErroreStornoNonAmmesso);
        }
        else
        {
            var ids = richiesta.IdFatture!;
            if (ids.Count == 0) errori.Add(ErroreSelezioneVuota);
            else
            {
                if (ids.Count > MaxFatture) errori.Add(ErroreSelezioneTroppoGrande);
                if (ids.Any(id => id <= 0)) errori.Add(ErroreIdNonValido);
                if (ids.Distinct().Count() != ids.Count) errori.Add(ErroreSelezioneDuplicata);
            }

            if (string.IsNullOrWhiteSpace(storno)) errori.Add(ErroreStornoMancante);
            else if (storno.Length > MaxStorno) errori.Add(ErroreStornoLungo);
        }

        return errori;
    }

    /// <summary>
    /// Controlli del re-invio sulle fatture lette dalla vista dettaglio. Non trovate → 404; fuori gruppo o
    /// non inviate (0 o NULL) → 400. <c>null</c> se tutto torna.
    /// </summary>
    public static InvioSapGeneraEsito? ValidaReInvio(
        InvioSapRichiestaGenerazione richiesta,
        IReadOnlyCollection<InvioSapFatturaCorrente> correnti)
    {
        ArgumentNullException.ThrowIfNull(richiesta);
        var ids = richiesta.IdFatture ?? [];
        var perId = correnti.GroupBy(f => f.IdFattura).ToDictionary(g => g.Key, g => g.First());

        var nonTrovate = ids.Where(id => !perId.ContainsKey(id)).ToList();
        if (nonTrovate.Count > 0)
            return InvioSapGeneraEsito.NonTrovate(nonTrovate);

        var selezionate = ids.Select(id => perId[id]).ToList();
        var tipologia = richiesta.TipologiaFattura!.Trim();

        var fuoriGruppo = selezionate
            .Where(f => f.Anno != richiesta.Anno
                     || f.Mese != richiesta.Mese
                     || !string.Equals(f.TipologiaFattura?.Trim(), tipologia, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.IdFattura)
            .ToList();
        if (fuoriGruppo.Count > 0)
            return InvioSapGeneraEsito.NonValida([ErroreFuoriGruppo], fuoriGruppo);

        var nonInviate = selezionate.Where(f => f.FatturaInviata != true).Select(f => f.IdFattura).ToList();
        if (nonInviate.Count > 0)
            return InvioSapGeneraEsito.NonValida([ErroreNonInviate], nonInviate);

        return null;
    }

    /// <summary>Filtri di griglia e download. <paramref name="annoCorrente"/> rende la regola testabile.</summary>
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

        if (!FiltroStatoInvioSap.IsValido(statoInvio)) errori.Add(ErroreStatoFiltro);

        return errori;
    }

    /// <summary>
    /// Filtri del dettaglio di un rigo: i quattro campi del rigo sono obbligatori; il dettaglio non esiste
    /// per i righi <c>DA_INVIARE</c> (C1: le fatture mai inviate non hanno storico).
    /// </summary>
    public static IReadOnlyList<string> ValidaFiltriDettaglio(
        int? anno, int? mese, string? tipologia, string? statoInvio,
        string? ragioneSociale, string? numeroFattura, int? page, int? pageSize)
    {
        var errori = new List<string>();
        if (anno is null || anno < AnnoMinimo || anno > 9999) errori.Add(ErroreAnno);
        if (mese is null or < 1 or > 12) errori.Add(ErroreMese);
        if (string.IsNullOrWhiteSpace(tipologia)) errori.Add(ErroreTipologia);

        var stato = FiltroStatoInvioSap.Normalizza(statoInvio);
        if (string.IsNullOrWhiteSpace(statoInvio) || stato == FiltroStatoInvioSap.TUTTE || !FiltroStatoInvioSap.IsValido(stato))
            errori.Add(ErroreStatoDettaglio);
        else if (stato == StatoInvioSap.DA_INVIARE)
            errori.Add(ErroreDettaglioDaInviare);

        if (ragioneSociale?.Length > MaxRagioneSociale) errori.Add(ErroreRagioneSociale);
        if (numeroFattura?.Length > MaxNumeroFattura) errori.Add(ErroreNumeroFattura);

        if (page is not null && page < 1) errori.Add(ErrorePaginazione);
        else if (pageSize is not null && pageSize is < 1 or > MaxPageSize) errori.Add(ErrorePaginazione);
        else if ((page is null) != (pageSize is null)) errori.Add(ErrorePaginazione);

        return errori;
    }
}
