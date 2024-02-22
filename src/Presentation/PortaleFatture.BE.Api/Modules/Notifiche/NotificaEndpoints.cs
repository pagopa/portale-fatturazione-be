using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.Notifiche;

public partial class NotificaModule : Module, IRegistrableModule
{ 
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
           .MapPost("api/notifiche/pagopa", GetPagoPANotificheByRicercaAsync)
           .WithName("Permette di ottenere le notiifche dell'ente per ricerca PagoPA")
           .SetOpenApi(Module.DatiNotificaLabelPagoPA)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/notifiche/pagopa/contestazione/{idNotifica}", GetPagoPAContestazioneAsync)
           .WithName("Permette di gestire i dati relativi ad una singola contestazione via PagoPA")
           .SetOpenApi(Module.DatiNotificaLabelPagoPA)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapPut("api/notifiche/pagopa/contestazione", UpdatePagoPAContestazioneAsync)
         .WithName("Permette di modificare i dati relativi alla contestazione via PagoPA.")
         .SetOpenApi(Module.DatiNotificaLabelPagoPA)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/notifiche/pagopa/documento/ricerca", GetPagoPANotificheRicercaDocumentAsync)
        .WithName("Permette di ottenere il file excel per le notifiche per ricerca via PagoPA.")
        .SetOpenApi(Module.DatiNotificaLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/notifiche/ente", GetNotificheByRicercaAsync)
        .WithName("Permette di ottenere le notiifche dell'ente per ricerca")
        .SetOpenApi(Module.DatiNotificaLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/notifiche/ente/documento/ricerca", GetNotificheRicercaDocumentAsync)
        .WithName("Permette di ottenere il file excel per le notifiche per ricerca")
        .SetOpenApi(Module.DatiNotificaLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapPost("api/notifiche/contestazione", CreateContestazioneAsync)
         .WithName("Permette di creare i dati relativi alla contestazione.")
         .SetOpenApi(Module.DatiNotificaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapPut("api/notifiche/contestazione", UpdateContestazioneAsync)
         .WithName("Permette di modificare i dati relativi alla contestazione.")
         .SetOpenApi(Module.DatiNotificaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/notifiche/contestazione/{idNotifica}", GetContestazioneAsync)
         .WithName("Permette di gestire i dati relativi ad una singola contestazione.")
         .SetOpenApi(Module.DatiNotificaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
} 