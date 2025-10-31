using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;
using PortaleFatture.BE.Core.Exceptions;

namespace PortaleFatture.BE.Infrastructure.Gateway;
public class MockSelfCareOnBoardingHttpClient : ISelfCareOnBoardingHttpClient
{
    public Task<(bool Success, string Message)> RecipientCodeVerification(EnteContrattoDto ente, string? codiceSDI, bool skipVerifica, CancellationToken ct = default)
    {
        try
        { 
           return Task.FromResult((true, string.Empty));
        }
        catch
        {
            var msg = "Fatal error reaching SelfCare OnBoarding!";
            throw new ValidationException(msg);
        } 
    }
}