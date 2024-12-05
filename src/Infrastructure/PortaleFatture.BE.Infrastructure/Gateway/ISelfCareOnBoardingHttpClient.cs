
using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;

namespace PortaleFatture.BE.Infrastructure.Gateway
{
    public interface ISelfCareOnBoardingHttpClient
    {
        Task<(bool Success, string Message)> RecipientCodeVerification(EnteContrattoDto ente, string? codiceSDI, CancellationToken ct = default);
    }
}