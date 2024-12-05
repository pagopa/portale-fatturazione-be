
using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;

namespace PortaleFatture.BE.Infrastructure.Gateway
{
    public interface ISupportAPIServiceHttpClient
    {
        Task<(bool Success, string Message)> UpdateRecipientCode(EnteContrattoDto ente, string? codiceSDI, CancellationToken ct = default);
    }
}