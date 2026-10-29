using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Sostituto di <see cref="ISynapseService"/> per i test HTTP: la pipeline Synapse non si raggiunge dal
/// container e comunque non va avviata da un test. L'esito e' impostabile per provare sia il 202 sia il
/// ramo "pipeline non avviata" (batch FALLITO, lock rilasciato, 500); ogni chiamata viene registrata per
/// asserire che il backend passi il <c>batchId</c> e il nome file (contratto BE-DATA).
///
/// Quando <c>ISynapseService</c> ricevera' il metodo <c>AvviaGenerazioneJsonSap</c> (PF-965, fase 3) va
/// implementato qui, registrando la richiesta in <see cref="Chiamate"/>.
/// </summary>
public sealed class FakeSynapseService : ISynapseService
{
    public static readonly FakeSynapseService Instance = new();

    private readonly object _lock = new();

    /// <summary>Esito restituito alle prossime chiamate (default: successo).</summary>
    public bool Esito { get; set; } = true;

    /// <summary>Ritardo artificiale, per allargare la finestra di concorrenza nei test sul lock.</summary>
    public TimeSpan Ritardo { get; set; } = TimeSpan.Zero;

    public List<object> Chiamate { get; } = [];

    public void Reset()
    {
        lock (_lock)
        {
            Esito = true;
            Ritardo = TimeSpan.Zero;
            Chiamate.Clear();
        }
    }

    public string? GetSynapseWorkspaceUrl() => "https://fake.dev.azuresynapse.net";

    public Task<bool> InviaASapFatture(string? pipelineName, FatturaInvioSap parameters)
        => Registra(parameters);

    public Task<bool> InviaASapFattureMultiplo(string? pipelineName, List<FatturaInvioSap> parameters)
        => Registra(parameters);

    private async Task<bool> Registra(object richiesta)
    {
        if (Ritardo > TimeSpan.Zero) await Task.Delay(Ritardo);
        lock (_lock) Chiamate.Add(richiesta);
        return Esito;
    }
}
