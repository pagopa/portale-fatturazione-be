using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.pagoPA.KPIPagamenti;

public partial class KPIPagamentiModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    { 
        endpointRouteBuilder
           .MapGet("api/v2/pagopa/kpipagamenti/matrice", GetKPIPagamentiMatrice)
           .WithName("Permette di visualizzare la matrice kpipagamenti")
           .SetOpenApi(Module.KPIPagamenti)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));


        endpointRouteBuilder
           .MapPost("api/v2/pagopa/kpipagamenti", PostKPIPagamenti)
           .WithName("Permette di visualizzare per ricerca kpipagamenti")
           .SetOpenApi(Module.KPIPagamenti)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/v2/pagopa/kpipagamenti/document", PostKPIPagamentiDocument)
          .WithName("Permette di visualizzare i dati relativi ai kpi pagamenti reports pagoPA via excel")
          .SetOpenApi(Module.KPIPagamenti)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
} 