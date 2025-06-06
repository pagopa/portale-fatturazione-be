﻿using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.Notifiche;

public partial class RelModule : Module, IRegistrableModule
{ 
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        #region selfcare
        endpointRouteBuilder
          .MapGet("api/rel/anni", GetAnniEnteRelAsync)
          .WithName("Permette visualizzare anni REL - Ente")
          .SetOpenApi(Module.DatiRelLabel)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/rel/mesi", PostMesiEnteRelAsync)
          .WithName("Permette visualizzare mesi REL per anno - Ente")
          .SetOpenApi(Module.DatiRelLabel)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/rel/tipologiafattura", PostTipologiaFatturaAsync)
        .WithName("Permette di verificare le tipologie fatture")
        .SetOpenApi(Module.DatiRelLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

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
        .MapGet("api/rel/pagopa/nonfatturate", GetRelNonFatturateExcelAsync)
        .WithName("Permette di scaricare l'excel delle rel non fatturate")
        .SetOpenApi(Module.DatiRelLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/rel/pagopa/tipologiafattura", PostTipologiaFatturaPagoPAAsync)
        .WithName("Permette di verificare le tipologie fatture pagoPA")
        .SetOpenApi(Module.DatiRelLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapGet("api/rel/pagopa/documento/download/{id}", GetDownloadPagoPAAsync)
        .WithName("Permette di scaricare il file rel lato pagoPA pdf firmato")
        .SetOpenApi(Module.DatiRelLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/rel/fatturabile", PostPagoPAFatturabileAsync)
        .WithName("Permette di cambiare lo stato fatturabile via PagoPA")
        .SetOpenApi(Module.DatiRelLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/rel/pagopa/firma/log", PostPagoPADownloadLogAsync)
        .WithName("Permette di scaricare il log pdf firmato via PagoPA")
        .SetOpenApi(Module.DatiRelLabelPagoPA)
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
        .WithName("Permette di ottenere le righe delle Rels dell'ente per ricerca pagoPA")
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

        endpointRouteBuilder
        .MapPost("api/rel/pagopa/quadratura/ricerca", GetPagoPAQuadraturaRicercaDocumentAsync)
        .WithName("Permette di ottenere il documento quadratura Rels dell'ente per ricerca pagoPA")
        .SetOpenApi(Module.DatiRelLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapGet("api/rel/pagopa/anni", GetAnniRelAsync)
          .WithName("Permette visualizzare anni REL - pagoPA")
          .SetOpenApi(Module.DatiRelLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/rel/pagopa/mesi", PostMesiRelAsync)
          .WithName("Permette visualizzare mesi REL per anno - pagoPA")
          .SetOpenApi(Module.DatiRelLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel)); 
        #endregion
    }
} 