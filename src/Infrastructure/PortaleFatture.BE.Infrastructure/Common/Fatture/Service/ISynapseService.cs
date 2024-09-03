
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Service;

public interface ISynapseService
{
    string? GetSynapseWorkspaceUrl(); 
    Task<bool> InviaASapFatture(string? pipelineName, FatturaInvioSap parameters);
}