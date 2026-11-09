using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

/// <summary>
/// Body di <c>createRun</c> per la generazione JSON (be-invio-sap.md §8.7). DATA chiede sicuramente
/// <c>batchId</c> e <c>idMessaggio</c>; <c>parametri</c> (tipologia e mese nel formato legacy) resta finche'
/// DATA non conferma di non usarlo (punto D-c).
/// </summary>
public sealed class InvioSapPipelineRequest
{
    [JsonPropertyName("batchId")]
    public int BatchId { get; init; }

    [JsonPropertyName("idMessaggio")]
    public long IdMessaggio { get; init; }

    [JsonPropertyName("parametri")]
    public List<PipelineParameter>? Parametri { get; init; }

    public static InvioSapPipelineRequest Crea(int batchId, long idMessaggio, string tipologia, int anno, int mese) => new()
    {
        BatchId = batchId,
        IdMessaggio = idMessaggio,
        Parametri = [new PipelineParameter { TipoFattura = tipologia, MesiDaFatturare = [$"{anno}{mese:00}"] }]
    };
}

/// <summary>Esito di <c>createRun</c>: il <c>runId</c> e' utile solo nei log.</summary>
public sealed record SynapseRunResult(bool Success, int? StatusCode, string? RunId);
