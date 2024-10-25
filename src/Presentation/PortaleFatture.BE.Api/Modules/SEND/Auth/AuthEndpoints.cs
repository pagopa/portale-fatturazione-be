using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.Auth;

public partial class AuthModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
           .MapGet("api/auth/selfcare/login", LoginSelfCareAsync)
           .WithName("Permette di autenticare un utente selfcare.")
           .SetOpenApi(Module.DatiAuthLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/auth/profilo", ProfiloAsync)
           .WithName("Permette di verificare un utente selfcare.")
           .SetOpenApi(Module.DatiAuthLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel)); 

        endpointRouteBuilder
           .MapPost("api/auth/pagopa/login", LoginPagoPAAsync)
           .WithName("Permette di verificare il autenticare un utente pagoPA.")
           .SetOpenApi(Module.DatiAuthLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel)); 
    }
} 