using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.pagoPA.Auth;

public partial class AuthModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    { 
        endpointRouteBuilder
           .MapPost("api/v2/auth/pagopa/login", LoginPagoPAProfiliAsync)
           .WithName("Permette di verificare il autenticare un utente pagoPA con prodotti.")
           .SetOpenApi(Module.DatiAuthLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
} 