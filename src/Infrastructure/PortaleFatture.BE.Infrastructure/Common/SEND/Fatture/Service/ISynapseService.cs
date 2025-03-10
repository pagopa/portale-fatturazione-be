using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service;

public interface ISynapseService
{
    string? GetSynapseWorkspaceUrl();
    Task<bool> InviaASapFatture(string? pipelineName, FatturaInvioSap parameters);
    Task<bool> InviaASapFattureMultiplo(string? pipelineName, List<FatturaInvioSap> parameters);
}