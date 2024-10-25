using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.Messaggi;

public partial class MessaggiModule : Module, IRegistrableModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
        .MapGet("api/messaggi/count", GetMessaggiPagoPACountAsync)
        .WithName("Permette di visualizzare il count messaggi non letti")
        .SetOpenApi(Module.DatiMessaggiPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/messaggi", PostMessaggiPagoPAAsync)
        .WithName("Permette di avere i messaggi relativi a un utente pagoPA")
        .SetOpenApi(Module.DatiMessaggiPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/messaggi/download", PostDownloadMessaggioPagoPAAsync)
        .WithName("Permette di scaricare il documento nel messaggio")
        .SetOpenApi(Module.DatiMessaggiPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/messaggi/read", PostReadMessaggioPagoPAAsync)
        .WithName("Permette di leggere il messaggio")
        .SetOpenApi(Module.DatiMessaggiPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
}