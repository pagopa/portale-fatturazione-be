namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;

// Response dell'Invio a SAP (be-invio-sap.md §3). Date in ora italiana (le viste e le tabelle sono in UTC).

public sealed class InvioSapGrigliaResponse
{
    /// <summary>Chiave del rigo: <c>TIPOLOGIA|anno|mese|STATO</c>.</summary>
    public string Id { get; init; } = string.Empty;
    public string TipologiaFattura { get; init; } = string.Empty;
    public int AnnoRiferimento { get; init; }
    public int MeseRiferimento { get; init; }
    public string StatoInvio { get; init; } = string.Empty;
    public int NumeroFatture { get; init; }
    public decimal ImportoTotale { get; init; }
}

public sealed class InvioSapDettaglioResponse
{
    public int Count { get; init; }
    public IReadOnlyList<InvioSapFatturaResponse> Fatture { get; init; } = [];
}

public sealed class InvioSapFatturaResponse
{
    public long IdFattura { get; init; }
    public string? NumeroFattura { get; init; }
    public long? Progressivo { get; init; }
    public string? TipoDocumento { get; init; }
    public string? TipologiaFattura { get; init; }
    public string? IdEnte { get; init; }
    public string? RagioneSociale { get; init; }
    public decimal Importo { get; init; }
    public string? IdDocumento { get; init; }
    public DateTime DataFattura { get; init; }
    public string StatoInvio { get; init; } = string.Empty;
    public string? Semestre { get; init; }
    public DateTime? DataUltimaGenerazione { get; init; }
    public string? NomeFile { get; init; }
    public int NumeroInvii { get; init; }
    public IReadOnlyList<InvioSapStoricoInvioResponse> StoricoInvii { get; init; } = [];
}

public sealed class InvioSapStoricoInvioResponse
{
    public int NInvio { get; init; }
    public DateTime? Data { get; init; }
    public string? Testo { get; init; }
    public string? Azione { get; init; }
}

public sealed class InvioSapGeneraResponse
{
    public int BatchId { get; init; }
    public string Stato { get; init; } = string.Empty;
    public DateTime DataAvvio { get; init; }
    public int NumeroFatture { get; init; }
    public IReadOnlyList<long> IdFatture { get; init; } = [];
}

public sealed class InvioSapConflittoResponse
{
    public string Motivo { get; init; } = string.Empty;
    public int? BatchId { get; init; }
    public DateTime? LockTimeout { get; init; }
}

public sealed class InvioSapLockResponse
{
    public string? Azione { get; init; }
    public int? BatchId { get; init; }
    public DateTime LockStart { get; init; }
    public DateTime LockTimeout { get; init; }
}

public sealed class InvioSapStatoResponse
{
    public string Codice { get; init; } = string.Empty;
    public string Descrizione { get; init; } = string.Empty;
}
