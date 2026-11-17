namespace PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

/// <summary>Una fattura da re-inviare, con la sua nota e il suo documento di storno: tutti obbligatori.</summary>
public sealed class InvioSapFatturaDaReinviare
{
    public long? IdFattura { get; init; }
    public string? Nota { get; init; }
    public string? NumeroDocumentoStorno { get; init; }
}

/// <summary>
/// Body di <c>PUT api/fatture/invio-sap/reinvia</c>, gia' deserializzato: un solo rigo (anno, mese, tipologia),
/// perche' il lock ammette un solo batch alla volta e la pipeline di DATA lavora su un solo periodo.
/// </summary>
public sealed class InvioSapRichiestaReinvio
{
    public int? Anno { get; init; }
    public int? Mese { get; init; }
    public string? TipologiaFattura { get; init; }
    public IReadOnlyList<InvioSapFatturaDaReinviare?>? Fatture { get; init; }

    /// <summary>
    /// La stessa selezione vista come re-invio del Genera: serve a riusare <see cref="InvioSapRegole.ValidaSelezione"/>
    /// (non trovate 404, fuori gruppo 400, non inviate 400). Da chiamare solo dopo una validazione formale senza errori.
    /// </summary>
    public InvioSapRichiestaGenerazione ComeGenerazione() => new()
    {
        Anno = Anno,
        Mese = Mese,
        TipologiaFattura = TipologiaFattura,
        StatoInvio = StatoInvioSap.INVIATE,
        IdFatture = (Fatture ?? []).Select(f => f!.IdFattura!.Value).ToList()
    };
}

/// <summary>
/// Regole pure del re-invio (rotta <c>reinvia</c>). Stessa precedenza del Genera: lock 409 (handler) → formale 400 →
/// non trovate 404 → fuori gruppo o non inviate 400. Nota e storno sono per fattura, e obbligatori su ognuna.
/// </summary>
public static class InvioSapRegoleReinvio
{
    public const string ErroreFatturaNonValida = "Fattura non valida: indicare idFattura, nota e numeroDocumentoStorno";
    public const string ErroreNotaMancante = "La nota e' obbligatoria per ogni fattura";
    public const string ErroreNotaLunga = "La nota supera i 2000 caratteri";
    public const string ErroreStornoMancante = "Il numero documento di storno e' obbligatorio per ogni fattura";
    public const string ErroreStornoLungo = "Il numero documento di storno supera i 255 caratteri";

    /// <summary>Tutto cio' che si vede dal solo body (e dall'utente): 400. Le fatture coinvolte sono nel messaggio.</summary>
    public static IReadOnlyList<string> ValidaFormale(InvioSapRichiestaReinvio richiesta, string? idUtente)
    {
        ArgumentNullException.ThrowIfNull(richiesta);
        var errori = new List<string>();

        if (richiesta.Anno is null || richiesta.Anno < InvioSapRegole.AnnoMinimo || richiesta.Anno > 9999) errori.Add(InvioSapRegole.ErroreAnno);
        if (richiesta.Mese is null or < 1 or > 12) errori.Add(InvioSapRegole.ErroreMese);
        if (string.IsNullOrWhiteSpace(richiesta.TipologiaFattura)) errori.Add(InvioSapRegole.ErroreTipologia);
        if (string.IsNullOrWhiteSpace(idUtente) || idUtente.Length > InvioSapRegole.MaxIdUtente) errori.Add(InvioSapRegole.ErroreUtente);

        var fatture = richiesta.Fatture ?? [];
        if (fatture.Count == 0)
        {
            errori.Add(InvioSapRegole.ErroreSelezioneVuota);
            return errori;
        }
        if (fatture.Count > InvioSapRegole.MaxFatture) errori.Add(InvioSapRegole.ErroreSelezioneTroppoGrande);

        if (fatture.Any(f => f is null)) errori.Add(ErroreFatturaNonValida);
        var presenti = fatture.Where(f => f is not null).Select(f => f!).ToList();

        if (presenti.Any(f => f.IdFattura is null or <= 0)) errori.Add(InvioSapRegole.ErroreIdNonValido);
        var ids = presenti.Where(f => f.IdFattura > 0).Select(f => f.IdFattura!.Value).ToList();
        if (ids.Distinct().Count() != ids.Count) errori.Add(InvioSapRegole.ErroreSelezioneDuplicata);

        AggiungiSeCe(errori, ErroreNotaMancante, presenti.Where(f => string.IsNullOrWhiteSpace(f.Nota)));
        AggiungiSeCe(errori, ErroreNotaLunga, presenti.Where(f => f.Nota?.Length > InvioSapRegole.MaxNota));
        AggiungiSeCe(errori, ErroreStornoMancante, presenti.Where(f => string.IsNullOrWhiteSpace(f.NumeroDocumentoStorno)));
        AggiungiSeCe(errori, ErroreStornoLungo, presenti.Where(f => f.NumeroDocumentoStorno?.Length > InvioSapRegole.MaxStorno));

        return errori;
    }

    /// <summary>Un solo messaggio per regola, con gli id delle fatture interessate (quelle con un id valido).</summary>
    private static void AggiungiSeCe(List<string> errori, string messaggio, IEnumerable<InvioSapFatturaDaReinviare> coinvolte)
    {
        var lista = coinvolte.ToList();
        if (lista.Count == 0) return;
        var ids = lista.Where(f => f.IdFattura > 0).Select(f => f.IdFattura!.Value).Distinct().ToList();
        errori.Add(ids.Count == 0 ? messaggio : $"{messaggio} (fatture: {string.Join(", ", ids)})");
    }
}
