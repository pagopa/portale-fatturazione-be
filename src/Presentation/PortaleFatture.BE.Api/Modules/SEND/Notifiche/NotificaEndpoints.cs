using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.Notifiche;

public partial class NotificaModule : Module, IRegistrableModule
{ 
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        #region pagoPA-selfcare
        endpointRouteBuilder
          .MapGet("api/notifiche/anni", GetAnniNotificaAsync)
          .WithName("Permette visualizzare anni notifiche conta - pagoPA")
          .SetOpenApi(Module.DatiNotificaLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/notifiche/mesi", PostMesiNotificaAsync)
          .WithName("Permette visualizzare mesi notifiche conta per anno - pagoPA")
          .SetOpenApi(Module.DatiNotificaLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion
        #region pagoPA

        endpointRouteBuilder
           .MapPost("api/v2/notifiche/pagopa", GetPagoPANotificheByRicercaAsyncv2)
           .WithName("Permette di ottenere le notifiche dell'ente per ricerca PagoPA v2")
           .SetOpenApi(Module.DatiNotificaLabelPagoPA)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/notifiche/pagopa", GetPagoPANotificheByRicercaAsync)
           .WithName("Permette di ottenere le notifiche dell'ente per ricerca PagoPA")
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
        .WithName("Permette di ottenere il file excel/csv per le notifiche per ricerca via PagoPA.")
        .SetOpenApi(Module.DatiNotificaLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion
        #region consolidatore
        endpointRouteBuilder
         .MapGet("api/notifiche/consolidatore/richiesta/count", GetConsolidatoriNotificheCountAsync)
         .WithName("Permette di tornare il count delle richieste effettuate dal consolidatore relative alle notifiche.")
         .SetOpenApi(Module.DatiNotificaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/notifiche/consolidatore", GetConsolidatoriNotificheByRicercaAsync)
        .WithName("Permette di ottenere le notifiche del consolidatore per ricerca")
        .SetOpenApi(Module.DatiNotificaLabelConsolidatori)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/notifiche/consolidatore/documento/ricerca", GetConsolidatoriNotificheRicercaDocumentAsync)
        .WithName("Permette di ottenere il file excel per le notifiche per ricerca via consolidatore.")
        .SetOpenApi(Module.DatiNotificaLabelConsolidatori)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/notifiche/consolidatore/contestazione/{idNotifica}", GetConsolidatoriContestazioneAsync)
         .WithName("Permette di gestire i dati relativi ad una singola contestazione via consolidatore.")
         .SetOpenApi(Module.DatiNotificaLabelConsolidatori)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapPut("api/notifiche/consolidatore/contestazione", UpdateConsolidatoreContestazioneAsync)
         .WithName("Permette di modificare i dati relativi alla contestazione via consolidatore.")
         .SetOpenApi(Module.DatiNotificaLabelConsolidatori)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion
        #region recapitista

        endpointRouteBuilder
         .MapGet("api/notifiche/recapitista/richiesta/count", GetRecapitistiNotificheCountAsync)
         .WithName("Permette di tornare il count delle richieste effettuate dal recapitista relative alle notifiche.")
         .SetOpenApi(Module.DatiNotificaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/notifiche/recapitista", GetRecapitistiNotificheByRicercaAsync)
        .WithName("Permette di ottenere le notifiche del recapitista per ricerca")
        .SetOpenApi(Module.DatiNotificaLabelRecapitisti)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/notifiche/recapitista/contestazione/{idNotifica}", GetRecapitistiContestazioneAsync)
         .WithName("Permette di gestire i dati relativi ad una singola contestazione via recapitista.")
         .SetOpenApi(Module.DatiNotificaLabelRecapitisti)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapPut("api/notifiche/recapitista/contestazione", UpdateRecapitistaContestazioneAsync)
         .WithName("Permette di modificare i dati relativi alla contestazione via recapitista.")
         .SetOpenApi(Module.DatiNotificaLabelRecapitisti)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/notifiche/recapitista/documento/ricerca", GetRecapitistaNotificheRicercaDocumentAsync)
        .WithName("Permette di ottenere il file excel per le notifiche per ricerca via recapitista.")
        .SetOpenApi(Module.DatiNotificaLabelRecapitisti)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion

        #region ente

        endpointRouteBuilder
        .MapPost("api/notifiche/ente", GetNotificheByRicercaAsync)
        .WithName("Permette di ottenere le notiifche dell'ente per ricerca")
        .SetOpenApi(Module.DatiNotificaLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/v2/notifiche/ente/documento/ricerca", GetNotificheRicercaDocumentAzureFunctionAsync)
        .WithName("Permette di ottenere il file excel/csv per le notifiche per ricerca via azure function")
        .SetOpenApi(Module.DatiNotificaLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapPost("api/notifiche/ente/documento/ricerca", GetNotificheRicercaDocumentAsync)
         .WithName("Permette di ottenere il file excel/csv per le notifiche per ricerca via query")
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

        endpointRouteBuilder
         .MapGet("api/notifiche/richiesta/count", GetRichiesteNotificheCountAsync)
         .WithName("Permette di tornare il count delle richieste effettuate relative alle notifiche.")
         .SetOpenApi(Module.DatiNotificaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));


        endpointRouteBuilder
         .MapPost("api/notifiche/richiesta", PostRichiesteNotificheAsync)
         .WithName("Permette di tornare le richieste effettuate relative alle notifiche.")
         .SetOpenApi(Module.DatiNotificaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel)); 

        endpointRouteBuilder
         .MapGet("api/notifiche/richiesta/download", GetRichiesteNotificheDownloadAsync)
         .WithName("Permette di tornare il report richieste effettuate relative alle notifiche per id report.")
         .SetOpenApi(Module.DatiNotificaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/notifiche/richiesta/verifica", PostVerificaNotificheDownloadAsync)
        .WithName("Permette di tornare la verifica per le notifiche id report.")
        .SetOpenApi(Module.DatiNotificaLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion
    }
} 