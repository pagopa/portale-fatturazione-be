using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service;

public interface ISynapseService
{
    string? GetSynapseWorkspaceUrl();
    Task<bool> InviaASapFatture(string? pipelineName, FatturaInvioSap parameters);
    Task<bool> InviaASapFattureMultiplo(string? pipelineName, List<FatturaInvioSap> parameters);

    /// <summary>
    /// Avvia la generazione JSON di un batch (PF-965). Il chiamante tiene aperta la transazione: il timeout
    /// HTTP e' esplicito e sotto quello del gateway. Non lancia: un errore e' <c>Success = false</c>.
    /// </summary>
    Task<SynapseRunResult> AvviaGenerazioneJsonSap(string? pipelineName, InvioSapPipelineRequest richiesta, CancellationToken ct = default);
}