using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.Notifiche;

public partial class RelModule : Module, IRegistrableModule
{ 
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        #region selfcare
        endpointRouteBuilder
        .MapPost("api/rel/firma/upload/{id}", PostUploadFirmaAsync)  
        .WithName("Permette caricare il file rel pdf firmato")
        .SetOpenApi(Module.DatiRelLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/rel/firma/log", PostDownloadLogAsync)
        .WithName("Permette di scaricare il log pdf firmato")
        .SetOpenApi(Module.DatiRelLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapGet("api/rel/firma/download/{id}", GetDownloadFirmaAsync) 
        .WithName("Permette di scaricare il file rel pdf firmato")
        .SetOpenApi(Module.DatiRelLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/rel/ente", GetRelTestataByRicercaAsync)
        .WithName("Permette di ottenere le Rel dell'ente per ricerca")
        .SetOpenApi(Module.DatiRelLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel)); 

         endpointRouteBuilder
        .MapGet("api/rel/ente/{id}", GetRelTestataByIdAsync)
        .WithName("Permette di ottenere le Rel dell'ente per id")
        .SetOpenApi(Module.DatiRelLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/rel/ente/download/{id}", DownloadRelDocumentoAsync)
         .WithName("Permette di scaricare il pdf relativo ad una specifica rel")
         .SetOpenApi(Module.DatiRelLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/rel/ente/documento/ricerca", GetRelRicercaDocumentAsync)
        .WithName("Permette di ottenere il documento Rels dell'ente per ricerca")
        .SetOpenApi(Module.DatiRelLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapGet("api/rel/ente/righe/{id}", GetRelRigheDocumentAsync)
        .WithName("Permette di ottenere le righe delle Rels dell'ente per ricerca")
        .SetOpenApi(Module.DatiRelLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion

        #region pagoPA
        endpointRouteBuilder
        .MapPost("api/rel/pagopa/firma/log", PostPagoPADownloadLogAsync)
        .WithName("Permette di scaricare il log pdf firmato via PagoPA")
        .SetOpenApi(Module.DatiRelLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/rel/pagopa", GetPagoPARelTestataByRicercaAsync)
        .WithName("Permette di ottenere le Rel dell'ente per ricerca pagoPA")
        .SetOpenApi(Module.DatiRelLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/rel/pagopa/documento/ricerca", GetPagoPARelRicercaDocumentAsync)
        .WithName("Permette di ottenere il documento Rels dell'ente per ricerca pagoPA")
        .SetOpenApi(Module.DatiRelLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/rel/pagopa/firma/download", PostPagoPADownloadFirmaZipAsync)
        .WithName("Permette di ottenere tutti i Rels firmati per ricerca pagoPA")
        .SetOpenApi(Module.DatiRelLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapGet("api/rel/pagopa/righe/{id}", GetPagoPARelRigheDocumentAsync)
        .WithName("Permette di ottenere le righe delle  Rels dell'ente per ricerca pagoPA")
        .SetOpenApi(Module.DatiRelLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
       .MapGet("api/rel/pagopa/{id}", GetPagoPARelTestataByIdAsync)
       .WithName("Permette di ottenere le Rel dell'ente per id PagoPA")
       .SetOpenApi(Module.DatiRelLabelPagoPA)
       .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel)); 

        endpointRouteBuilder
        .MapGet("api/rel/pagopa/firma/download/{id}", GetPagoPADownloadFirmaAsync)
        .WithName("Permette di scaricare il file rel pdf firmato via PagoPA")
        .SetOpenApi(Module.DatiRelLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion
    }
} 