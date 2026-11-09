namespace PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

/// <summary>
/// Esito del Genera. Gli esiti attesi viaggiano tipizzati e non come eccezioni, perche' il gestore globale
/// traduce <c>DomainException</c> in 500. La traduzione in HTTP sta nell'API (be-invio-sap.md §11).
/// </summary>
public enum InvioSapGeneraEsitoTipo
{
    /// <summary>202: batch creato e pipeline avviata.</summary>
    Ok,
    /// <summary>400: richiesta non valida.</summary>
    NonValida,
    /// <summary>404: fatture inesistenti o rigo senza fatture da inviare.</summary>
    NonTrovate,
    /// <summary>409: lock attivo, lock non rilasciato da DATA o rigo cambiato (v. <see cref="InvioSapGeneraEsito.Motivo"/>).</summary>
    Conflitto,
    /// <summary>500: Synapse non ha avviato la pipeline; transazione annullata, nulla scritto.</summary>
    PipelineFallita
}

/// <summary>Motivi del 409.</summary>
public static class InvioSapMotivoConflitto
{
    /// <summary>Un lock e' attivo sulla vista, o un altro Genera ha inserito il lock un istante prima.</summary>
    public const string LOCK_ATTIVO = "LOCK_ATTIVO";
    /// <summary>La riga di lock e' scaduta ma DATA non l'ha ancora cancellata: la PK rifiuta l'INSERT.</summary>
    public const string LOCK_NON_RILASCIATO = "LOCK_NON_RILASCIATO";
    /// <summary>Il rigo della griglia e' cambiato fra la lettura e l'INSERT del dettaglio.</summary>
    public const string GRUPPO_CAMBIATO = "GRUPPO_CAMBIATO";
}

public sealed class InvioSapGeneraEsito
{
    public InvioSapGeneraEsitoTipo Tipo { get; init; }
    public string? Motivo { get; init; }
    public IReadOnlyList<string> Errori { get; init; } = [];
    public IReadOnlyList<long> IdFattureCoinvolte { get; init; } = [];

    public int? BatchId { get; init; }
    public DateTime? DataAvvioUtc { get; init; }
    public int? NumeroFatture { get; init; }
    public IReadOnlyList<long> IdFatture { get; init; } = [];

    public int? LockBatchId { get; init; }
    public DateTime? LockTimeoutUtc { get; init; }

    public static InvioSapGeneraEsito NonValida(IReadOnlyList<string> errori, IReadOnlyList<long>? coinvolte = null) =>
        new() { Tipo = InvioSapGeneraEsitoTipo.NonValida, Errori = errori, IdFattureCoinvolte = coinvolte ?? [] };

    public static InvioSapGeneraEsito NonTrovate(IReadOnlyList<long>? coinvolte = null) =>
        new() { Tipo = InvioSapGeneraEsitoTipo.NonTrovate, IdFattureCoinvolte = coinvolte ?? [] };

    public static InvioSapGeneraEsito Conflitto(string motivo, int? lockBatchId = null, DateTime? lockTimeoutUtc = null) =>
        new() { Tipo = InvioSapGeneraEsitoTipo.Conflitto, Motivo = motivo, LockBatchId = lockBatchId, LockTimeoutUtc = lockTimeoutUtc };

    public static InvioSapGeneraEsito PipelineFallita() =>
        new() { Tipo = InvioSapGeneraEsitoTipo.PipelineFallita };
}
