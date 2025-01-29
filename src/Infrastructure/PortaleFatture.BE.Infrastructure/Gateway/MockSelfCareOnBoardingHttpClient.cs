using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;
using PortaleFatture.BE.Core.Exceptions;

namespace PortaleFatture.BE.Infrastructure.Gateway;
public class MockSelfCareOnBoardingHttpClient : ISelfCareOnBoardingHttpClient
{
    public Task<(bool Success, string Message)> RecipientCodeVerification(EnteContrattoDto ente, string? codiceSDI, bool skipVerifica, CancellationToken ct = default)
    {
        try
        {

            var status = codiceSDI == "0000000" ? 1 : 2;
            switch (status)
            {
                case 1:
                    return  Task.FromResult((true, string.Empty));
                case 2:
                    {
                        var msg = "Il codice inserito è associato al codice fiscale di un ente che non ha il servizio di fatturazione attivo.";
                        return Task.FromResult((false, msg));
                    }
                default:
                    break;
            }
        }
        catch
        {
            var msg = "Fatal error reaching SelfCare OnBoarding!";
            throw new ValidationException(msg);
        }
        return Task.FromResult((false, "Si è verificato un errore nella verifica del codice SDI."));
    }
}