using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.pagoPA.Auth;

public partial class PSPModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    { 
        endpointRouteBuilder
           .MapPost("api/v2/pagopa/psps", PostPSPList)
           .WithName("Permette di visualizzare i dati relativi ai PSP.")
           .SetOpenApi(Module.PSP)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/v2/pagopa/psps/name", PostPSPListByName)
           .WithName("Permette di visualizzare i dati relativi ai PSP per ricerca con nome.")
           .SetOpenApi(Module.PSP)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/v2/pagopa/psps/document", PostPSPDocumentListByName)
           .WithName("Permette di scaricare il file excel relativo ai PSP per ricerca con nome.")
           .SetOpenApi(Module.PSP)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
} 