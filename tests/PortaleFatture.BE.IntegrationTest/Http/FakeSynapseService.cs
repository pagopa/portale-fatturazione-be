using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Sostituto di <see cref="ISynapseService"/> nei test: la pipeline Synapse non si raggiunge dal container e
/// comunque non va avviata da un test. Permette di simulare un esito KO, un ritardo (per allargare la finestra
/// di concorrenza sul lock) e un'eccezione, e registra ogni richiesta, cosi' i test verificano che il body di
/// <c>createRun</c> contenga <c>batchId</c> e <c>idMessaggio</c> uguali a quelli scritti (be-invio-sap.md §8.7).
/// </summary>
public sealed class FakeSynapseService : ISynapseService
{
    public static readonly FakeSynapseService Instance = new();

    private readonly object _lock = new();

    /// <summary>Esito restituito alle prossime chiamate (default: successo).</summary>
    public bool Esito { get; set; } = true;

    /// <summary>Ritardo artificiale prima di rispondere.</summary>
    public TimeSpan Ritardo { get; set; } = TimeSpan.Zero;

    /// <summary>Se valorizzata, <see cref="AvviaGenerazioneJsonSap"/> la lancia (eccezione imprevista a meta' transazione).</summary>
    public Exception? Eccezione { get; set; }

    public List<object> Chiamate { get; } = [];

    public IReadOnlyList<InvioSapPipelineRequest> RichiesteGenerazione
    {
        get { lock (_lock) return Chiamate.OfType<InvioSapPipelineRequest>().ToList(); }
    }

    public void Reset()
    {
        lock (_lock)
        {
            Esito = true;
            Ritardo = TimeSpan.Zero;
            Eccezione = null;
            Chiamate.Clear();
        }
    }

    public string? GetSynapseWorkspaceUrl() => "https://fake.dev.azuresynapse.net";

    public Task<bool> InviaASapFatture(string? pipelineName, FatturaInvioSap parameters)
        => Registra(parameters);

    public Task<bool> InviaASapFattureMultiplo(string? pipelineName, List<FatturaInvioSap> parameters)
        => Registra(parameters);

    public async Task<SynapseRunResult> AvviaGenerazioneJsonSap(string? pipelineName, InvioSapPipelineRequest richiesta, CancellationToken ct = default)
    {
        if (Eccezione is not null) throw Eccezione;
        var ok = await Registra(richiesta);
        return new SynapseRunResult(ok, ok ? 200 : 500, ok ? $"run-{richiesta.BatchId}" : null);
    }

    private async Task<bool> Registra(object richiesta)
    {
        if (Ritardo > TimeSpan.Zero) await Task.Delay(Ritardo);
        lock (_lock) Chiamate.Add(richiesta);
        return Esito;
    }
}
