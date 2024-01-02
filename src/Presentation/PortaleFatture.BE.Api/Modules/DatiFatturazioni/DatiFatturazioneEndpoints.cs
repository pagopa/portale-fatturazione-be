using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure; 

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni;

partial class DatiFatturazioneModule : Module, IRegistrableModule
{ 
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
        .MapPost("api/datifatturazione/pagopa/ricerca", DatiFatturazioneByDescrizioneAsync)
        .WithName("Permette di ottenere i dati fatturazione enti per ricerca descrizione via utente PagoPA")
        .SetOpenApi(Module.DatiFatturazioneLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/datifatturazione/pagopa", CreatePagoPaDatiFatturazioneAsync)
        .WithName("Permette di salvare i dati fatturazione via utente PagoPA.")
        .SetOpenApi(Module.DatiFatturazioneLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapGet("api/datifatturazione/pagopa/ente", GetPagoPADatiFatturazioneByIdEnteAsync)
        .WithName("Permette di recuperare i dati fatturazione per id ente via utente PagoPA.")
        .SetOpenApi(Module.DatiFatturazioneLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPut("api/datifatturazione/pagopa", UpdatePagoPADatiFatturazioneAsync)
        .WithName("Permette di modificare i dati di fatturazione via utente PagoPA.")
        .SetOpenApi(Module.DatiFatturazioneLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/datifatturazione", CreateDatiFatturazioneAsync)
        .WithName("Permette di salvare i dati di fatturazione.")
        .SetOpenApi(Module.DatiFatturazioneLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPut("api/datifatturazione", UpdateDatiFatturazioneAsync)
        .WithName("Permette di modificare i dati di fatturazione.")
        .SetOpenApi(Module.DatiFatturazioneLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapGet("api/datifatturazione/ente", GetDatiFatturazioneByIdEnteAsync)
        .WithName("Permette di recuperare il dato relativo ai dati fatturazione per id ente.")
        .SetOpenApi(Module.DatiFatturazioneLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
} 