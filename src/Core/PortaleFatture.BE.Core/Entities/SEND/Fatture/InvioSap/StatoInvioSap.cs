namespace PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

/// <summary>
/// Stato di invio a SAP di una fattura (PF-735 RF-7). Non e' una colonna di <c>pfd.FattureTestata</c>: e'
/// derivato dalla vista <c>be.vwInvioSAPDettaglio</c> a partire da <c>FatturaInviata</c> e dai batch di
/// generazione (<c>pfw.RegistroInvioSAP</c> / <c>pfw.DettaglioInvioSAP</c>). Costanti stringa, come
/// <see cref="Messaggi.TipologiaDocumento"/>, perche' viaggiano cosi' nel contratto API e nelle viste.
/// </summary>
public static class StatoInvioSap
{
    /// <summary>Mai inclusa in un file JSON generato.</summary>
    public const string DA_INVIARE = "DA_INVIARE";

    /// <summary>Selezionata in un batch che il processo DATA non ha ancora chiuso.</summary>
    public const string IN_ELABORAZIONE = "IN_ELABORAZIONE";

    /// <summary>Inclusa almeno una volta in un file JSON (un re-invio la lascia INVIATA).</summary>
    public const string INVIATA = "INVIATA";

    public static readonly string[] Tutti = [DA_INVIARE, IN_ELABORAZIONE, INVIATA];

    public static bool IsValido(string? stato) =>
        stato is not null && Array.IndexOf(Tutti, stato) >= 0;
}

/// <summary>Stato di un batch di generazione JSON (<c>pfw.RegistroInvioSAP.stato</c>).</summary>
public static class StatoBatchSap
{
    public const string IN_ELABORAZIONE = "IN_ELABORAZIONE";
    public const string COMPLETATO = "COMPLETATO";
    public const string FALLITO = "FALLITO";

    public static readonly string[] Tutti = [IN_ELABORAZIONE, COMPLETATO, FALLITO];

    /// <summary>Un batch terminale non blocca piu' il lock globale, qualunque cosa dica la riga di lock.</summary>
    public static bool IsTerminale(string? stato) =>
        stato is COMPLETATO or FALLITO;
}

/// <summary>
/// Valori del filtro "Stato" della griglia Invio a SAP. Distinto da <see cref="StatoInvioSap"/> perche'
/// include <c>TUTTE</c> e perche' il filtro "re-inviate" (RF-13) e' un booleano a parte: una fattura
/// re-inviata resta <c>INVIATA</c>.
/// </summary>
public static class FiltroStatoInvioSap
{
    public const string TUTTE = "TUTTE";
    public const string DA_INVIARE = "DA_INVIARE";
    public const string IN_ELABORAZIONE = "IN_ELABORAZIONE";
    public const string INVIATE = "INVIATE";

    public static readonly string[] Tutti = [TUTTE, DA_INVIARE, IN_ELABORAZIONE, INVIATE];

    /// <summary>Assente o vuoto vale <c>TUTTE</c>; il confronto ignora spazi e maiuscole.</summary>
    public static string Normalizza(string? filtro) =>
        string.IsNullOrWhiteSpace(filtro) ? TUTTE : filtro.Trim().ToUpperInvariant();

    public static bool IsValido(string? filtro) =>
        Array.IndexOf(Tutti, Normalizza(filtro)) >= 0;

    /// <summary>Lo stato fattura corrispondente, o <c>null</c> per <c>TUTTE</c>.</summary>
    public static string? StatoFattura(string? filtro) => Normalizza(filtro) switch
    {
        DA_INVIARE => StatoInvioSap.DA_INVIARE,
        IN_ELABORAZIONE => StatoInvioSap.IN_ELABORAZIONE,
        INVIATE => StatoInvioSap.INVIATA,
        _ => null
    };
}
