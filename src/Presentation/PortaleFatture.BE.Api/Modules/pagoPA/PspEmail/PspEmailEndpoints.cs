using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.pagoPA.PspEmail;
 
public partial class PspEmailModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
           .MapPost("api/v2/pagopa/pspemail", PostPspEmail)
           .WithName("Permette di visualizzare psp email")
           .SetOpenApi(Module.PSPEmail)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
 
        endpointRouteBuilder
          .MapPost("api/v2/pagopa/pspemail/document", PostPspEmailDocument)
          .WithName("Permette di visualizzare psp email via excel")
          .SetOpenApi(Module.PSPEmail)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
}