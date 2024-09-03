using Microsoft.AspNetCore.Cors;
using Polly;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.Fatture;

public partial class FattureModule : Module, IRegistrableModule
{
    #region pagoPA
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
           .MapPost("api/fatture/resetta/pipeline", PostResettaFatturePipelineSapAsync)
           .WithName("Permette di resettare le fatture per invio a SAP")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));


        endpointRouteBuilder
           .MapPost("api/fatture/invio/pipeline", PostFatturePipelineSapAsync)
           .WithName("Permette di chiamare la pipeline per invio a SAP")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/invio/sap", PostFattureInvioSapAsync)
           .WithName("Permette di ottenere l'invio sap e numero fatture")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));


        endpointRouteBuilder
           .MapPost("api/fatture", PostFattureByRicercaAsync)
           .WithName("Permette di ottenere le fatture per ricerca")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/download", PostFattureExcelByRicercaAsync)
           .WithName("Permette di ottenere le fatture excel per ricerca")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/report/prenotazione", PostFatturePrenotazioneReportByRicercaAsync)
           .WithName("Permette di ottenere la prenotazione fatture excel-report per ricerca")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/report", PostFattureReportByRicercaAsync)
           .WithName("Permette di ottenere le fatture excel-report per ricerca")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/tipologia", PostTipologiaFatture)
           .WithName("Permette di visualizzare la tipologia fatture")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/cancellazione", PostCancellazioneFatture)
           .WithName("Permette di cancellare o ripristinare le fatture non inviate")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion

        #region ente
        endpointRouteBuilder
             .MapPost("api/fatture/ente/tipologia", PostTipologiaEnteFatture)
               .WithName("Permette di visualizzare la tipologia fatture per ente")
               .SetOpenApi(Module.DatiFattureEnti)
               .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/ente", PostFattureEnteByRicercaAsync)
           .WithName("Permette di ottenere le fatture per ricerca singolo ente")
           .SetOpenApi(Module.DatiFattureEnti)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/ente/download", PostFattureEnteExcelByRicercaAsync)
           .WithName("Permette di ottenere le fatture excel per ricerca ente")
           .SetOpenApi(Module.DatiFattureEnti)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion
    }
}