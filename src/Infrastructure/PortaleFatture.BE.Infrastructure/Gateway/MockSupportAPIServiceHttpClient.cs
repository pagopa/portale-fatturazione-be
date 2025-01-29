using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;

namespace PortaleFatture.BE.Infrastructure.Gateway;

public class MockSupportAPIServiceHttpClient : ISupportAPIServiceHttpClient
{
    public Task<(bool Success, string Message)> UpdateRecipientCode(EnteContrattoDto ente, string? codiceSDI, CancellationToken ct = default)
    {
        //var msg = "La modifica richiesta non può essere attualmente effettuata.";
        return Task.FromResult((true, string.Empty));

        //try
        //{ 
        //    var status = (codiceSDI == "0QKV5R" || codiceSDI== "10KQF4" || codiceSDI == "UFX4N3") ? 1 : 2;
        //    return status switch
        //    {
        //        1 => Task.FromResult((true, string.Empty)),
        //        _ => Task.FromResult((false, msg)),
        //    };
        //}
        //catch
        //{ 
        //    return Task.FromResult((false, msg));
        //}  
    }
} 