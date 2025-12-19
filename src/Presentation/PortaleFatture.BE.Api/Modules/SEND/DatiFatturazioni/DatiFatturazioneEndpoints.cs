using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure; 

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni;

partial class DatiFatturazioneModule : Module, IRegistrableModule
{ 
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        #region PagoPA
        endpointRouteBuilder
        .MapGet("api/datifatturazione/pagopa/tipologiacontratto", GetTipologiaContratto)
        .WithName("Permette di ottenere la tipologia contratto dati fatturazione aderenti via utente PagoPA.")
        .SetOpenApi(Module.DatiFatturazioneLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/datifatturazione/pagopa/documento/ricerca", PagoPADatiFatturazioneByDescrizioneDocumentAsync)
        .WithName("Permette di ottenere il file excel per la fatturazione enti per ricerca descrizione via utente PagoPA.")
        .SetOpenApi(Module.DatiFatturazioneLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/datifatturazione/pagopa/ricerca", PagoPADatiFatturazioneByDescrizioneAsync)
        .WithName("Permette di ottenere i dati fatturazione enti per ricerca descrizione via utente PagoPA.")
        .SetOpenApi(Module.DatiFatturazioneLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/datifatturazione/pagopa", CreatePagoPaDatiFatturazioneAsync)
        .WithName("Permette di salvare i dati fatturazione via utente PagoPA.")
        .SetOpenApi(Module.DatiFatturazioneLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapGet("api/datifatturazione/pagopa/ente", GetPagoPADatiFatturazioneByIdEnteAsync)
        .WithName("Permette di recuperare i dati fatturazione per id ente via utente PagoPA.")
        .SetOpenApi(Module.DatiFatturazioneLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPut("api/datifatturazione/pagopa", UpdatePagoPADatiFatturazioneAsync)
        .WithName("Permette di modificare i dati di fatturazione via utente PagoPA.")
        .SetOpenApi(Module.DatiFatturazioneLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/datifatturazione/pagopa/codiceSDI", VerificaCodiceSDIDatiFatturazione)
        .WithName("Permette di verificare il codice SDI via utente PagoPA.")
        .SetOpenApi(Module.DatiFatturazioneLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));


        endpointRouteBuilder
        .MapGet("api/datifatturazione/pagopa/ente/contractCodiceSDI", GetContractCodiceSDIAsync)
        .WithName("Permette di recuperare il contract codice SDI via utente PagoPA.")
        .SetOpenApi(Module.DatiFatturazioneLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion

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

        endpointRouteBuilder
        .MapGet("api/datifatturazione/ente/contractCodiceSDI", GetContractCodiceSDIByIdEnteAsync)
        .WithName("Permette di recuperare il contract codice SDI per id ente.")
        .SetOpenApi(Module.DatiFatturazioneLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/datifatturazione/codiceSDI", VerificaCodiceSDIDatiFatturazioneEnte)
        .WithName("Permette di verificare il codice SDI via Ente.")
        .SetOpenApi(Module.DatiFatturazioneLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
} 