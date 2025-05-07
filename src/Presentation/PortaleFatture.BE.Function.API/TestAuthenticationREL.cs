using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.Api.Models;

namespace PortaleFatture.BE.Function.Api;

//public class GetRELData(ILoggerFactory loggerFactory)
//{
//    private readonly ILogger _logger = loggerFactory.CreateLogger<GetRELData>();
  
//    [Function("TestAuthenticationREL")]
//    public async Task<RELDataResponse> RunAsync([ActivityTrigger] RELDataRequest req)
//    {
//        await Task.Delay(TimeSpan.FromSeconds(10));
//        var rel  = new RELDataResponse
//        {
//            InternalOrganizationId = Guid.NewGuid(),
//            ContractId = Guid.NewGuid(),
//            TipologiaFattura = "PRIMO SALDO",
//            Year = 2024,
//            Month = 12,
//            TotaleAnalogico = 0.00m,
//            TotaleDigitale = 1.00m,
//            TotaleNotificheAnalogiche = 0,
//            TotaleNotificheDigitali = 1,
//            Totale = 1.00m,
//            Iva = 22.00m,
//            TotaleAnalogicoIva = 0.00m,
//            TotaleDigitaleIva = 1.22m,
//            TotaleIva = 1.22m,
//            Caricata = null,
//            AsseverazioneTotaleAnalogico = null,
//            AsseverazioneTotaleDigitale = null,
//            AsseverazioneTotaleNotificheAnalogiche = null,
//            AsseverazioneTotaleNotificheDigitali = null,
//            AsseverazioneTotale = null,
//            AsseverazioneTotaleAnalogicoIva = null,
//            AsseverazioneTotaleDigitaleIva = null,
//            AsseverazioneTotaleIva = null,
//            RelFatturata = true
//        };
//        _logger.LogInformation(rel.Serialize());
//        return rel;
//    } 
//}