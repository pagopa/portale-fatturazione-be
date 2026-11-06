namespace PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

/// <summary>
/// Stato di invio a SAP di una fattura nella pagina "Invio a SAP" (PF-735). Non e' una colonna: e'
/// <c>pfd.FattureTestata.FatturaInviata</c>, esposta da <c>be.vwInvioSAPGriglia</c> come <c>StatoFattura</c>.
/// Regola di DATA (05/11/2026): 0 = non inviata, 1 = inviata, NULL = in elaborazione.
/// </summary>
public static class StatoInvioSap
{
    public const string DA_INVIARE = "DA_INVIARE";
    public const string INVIATE = "INVIATE";
    public const string IN_ELABORAZIONE = "IN_ELABORAZIONE";

    public static readonly string[] Tutti = [DA_INVIARE, INVIATE, IN_ELABORAZIONE];

    /// <summary>Codice del contratto API a partire dal valore del DB.</summary>
    public static string DaFatturaInviata(bool? fatturaInviata) => fatturaInviata switch
    {
        false => DA_INVIARE,
        true => INVIATE,
        null => IN_ELABORAZIONE
    };
}

/// <summary>Righe di <c>pfw.TipoStatoFattura</c> usate dall'invio (gli stati 0-3 sono di Gestione Fatture).</summary>
public static class StatoFatturaSap
{
    public const int NonInviata = 4;
    public const int Inviata = 5;
}

/// <summary>Valori di <c>pfw.RegistroInvioSAP.Stato</c>. Il BE scrive solo il primo, DATA gli altri due.</summary>
public static class StatoBatchSap
{
    public const string IN_ELABORAZIONE = "IN_ELABORAZIONE";
    public const string COMPLETATO = "COMPLETATO";
    public const string ERRORE = "ERRORE";

    public static readonly string[] Tutti = [IN_ELABORAZIONE, COMPLETATO, ERRORE];
}

/// <summary>Valori del filtro "Stato" della griglia: i tre stati piu' <c>TUTTE</c>.</summary>
public static class FiltroStatoInvioSap
{
    public const string TUTTE = "TUTTE";
    public const string DA_INVIARE = StatoInvioSap.DA_INVIARE;
    public const string INVIATE = StatoInvioSap.INVIATE;
    public const string IN_ELABORAZIONE = StatoInvioSap.IN_ELABORAZIONE;

    public static readonly string[] Tutti = [TUTTE, DA_INVIARE, INVIATE, IN_ELABORAZIONE];

    /// <summary>Codice e descrizione, nell'ordine di <c>GET api/fatture/invio-sap/stati</c>.</summary>
    public static readonly IReadOnlyList<(string Codice, string Descrizione)> Descrizioni =
    [
        (TUTTE, "Tutte"),
        (DA_INVIARE, "Da inviare"),
        (INVIATE, "Inviate"),
        (IN_ELABORAZIONE, "In elaborazione")
    ];

    /// <summary>Assente o vuoto vale <c>TUTTE</c>; il confronto ignora spazi e maiuscole.</summary>
    public static string Normalizza(string? filtro) =>
        string.IsNullOrWhiteSpace(filtro) ? TUTTE : filtro.Trim().ToUpperInvariant();

    public static bool IsValido(string? filtro) =>
        Array.IndexOf(Tutti, Normalizza(filtro)) >= 0;
}
