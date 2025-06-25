using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche;

public partial class ContestazioniModule : Module, IRegistrableModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        #region pagoPA
#if DEBUG
        endpointRouteBuilder
          .MapPost("api/notifiche/pagopa/contestazioni/testupload", UploadTestContestazioniAsync)
          .WithName("Da non utilizzare. Solo per test upload in parallelo.")
          .AddEndpointFilter(async (context, next) =>
          {
              return await next(context);
          })
          .DisableAntiforgery()
          .SetOpenApi(Module.DatiContestazioniLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
#endif

        endpointRouteBuilder
          .MapGet("api/notifiche/pagopa/contestazioni/reports", GetReportsContestazioniAsync)
          .WithName("Permette visualizzare report steps attività contestazioni per ente  - pagoPA")
          .SetOpenApi(Module.DatiContestazioniLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapGet("api/notifiche/pagopa/contestazioni/steps", GetStepsContestazioniAsync)
          .WithName("Permette visualizzare gli steps attività contestazioni")
          .SetOpenApi(Module.DatiContestazioniLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/notifiche/pagopa/contestazioni/upload", UploadContestazioniAsync)
          .WithName("Permette di caricare le contestazioni lato pagoPA")
          .AddEndpointFilter(async (context, next) =>
          { 
              return await next(context);
          }) 
          .DisableAntiforgery()
          .SetOpenApi(Module.DatiContestazioniLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapGet("api/notifiche/pagopa/contestazioni/anni", GetAnniContestazioniAsync)
          .WithName("Permette visualizzare anni notifiche - pagoPA")
          .SetOpenApi(Module.DatiContestazioniLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/notifiche/pagopa/contestazioni/mesi", PostMesiContestazioniAsync)
          .WithName("Permette visualizzare mesi notifiche per anno - pagoPA")
          .SetOpenApi(Module.DatiContestazioniLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/notifiche/pagopa/contestazioni/enti", PostEntiContestazioniAsync)
          .WithName("Permette visualizzare enti contestazione - pagoPA")
          .SetOpenApi(Module.DatiContestazioniLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/notifiche/pagopa/contestazioni/recap", PostRecapContestazioniAsync)
          .WithName("Permette visualizzare recap contestazioni per ente  - pagoPA")
          .SetOpenApi(Module.DatiContestazioniLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/notifiche/pagopa/contestazioni/reports", PostReportsContestazioniAsync)
          .WithName("Permette visualizzare reports attività contestazioni per ente  - pagoPA")
          .SetOpenApi(Module.DatiContestazioniLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapGet("api/notifiche/pagopa/contestazioni/reports/steps", GetReportsStepsContestazioniAsync)
          .WithName("Permette visualizzare reports steps attività contestazioni per ente  - pagoPA")
          .SetOpenApi(Module.DatiContestazioniLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapGet("api/notifiche/pagopa/contestazioni/tiporeport", GetTipoReportContestazioniAsync)
          .WithName("Permette visualizzare tipologia report contestazione - pagoPA")
          .SetOpenApi(Module.DatiContestazioniLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/notifiche/pagopa/contestazioni/reports/document", PostReportsDocumentContestazioniAsync)
          .WithName("Permette di avere il sas token del report  - pagoPA")
          .SetOpenApi(Module.DatiContestazioniLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion

        #region enti
        endpointRouteBuilder
          .MapGet("api/notifiche/enti/contestazioni/anni", GetAnniContestazioniEntiAsync)
          .WithName("Permette visualizzare anni notifiche - Enti")
          .SetOpenApi(Module.DatiContestazioniLabelEnti)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/notifiche/enti/contestazioni/mesi", PostMesiContestazioniEntiAsync)
          .WithName("Permette visualizzare mesi notifiche per anno - Enti")
          .SetOpenApi(Module.DatiContestazioniLabelEnti)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));


        endpointRouteBuilder
          .MapGet("api/notifiche/enti/contestazioni/tiporeport", GetTipoReportContestazioniEntiAsync)
          .WithName("Permette visualizzare tipologia report contestazione - Enti")
          .SetOpenApi(Module.DatiContestazioniLabelEnti)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapGet("api/notifiche/enti/contestazioni/steps", GetStepsContestazioniEnteAsync)
          .WithName("Permette visualizzare gli steps attività contestazioni ente")
          .SetOpenApi(Module.DatiContestazioniLabelEnti)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/notifiche/enti/contestazioni/reports", PostReportsContestazioniEnteAsync)
          .WithName("Permette visualizzare reports attività contestazioni per ente  - Ente")
          .SetOpenApi(Module.DatiContestazioniLabelEnti)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapGet("api/notifiche/enti/contestazioni/reports/steps", GetReportsStepsContestazioniEnteAsync)
          .WithName("Permette visualizzare reports steps attività contestazioni per ente  - Ente")
          .SetOpenApi(Module.DatiContestazioniLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/notifiche/enti/contestazioni/recap", PostRecapContestazioniEnteAsync)
          .WithName("Permette visualizzare recap contestazioni per ente  - Ente")
          .SetOpenApi(Module.DatiContestazioniLabelEnti)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/notifiche/enti/contestazioni/upload", UploadContestazioniEnteAsync)
          .WithName("Permette di caricare le contestazioni lato Ente")
          .AddEndpointFilter(async (context, next) =>
          {
              return await next(context);
          })
          .DisableAntiforgery()
          .SetOpenApi(Module.DatiContestazioniLabelEnti)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));


        endpointRouteBuilder
          .MapPost("api/notifiche/enti/contestazioni/reports/document", PostReportsDocumentContestazioniEnteAsync)
          .WithName("Permette di avere il sas token del report  - Ente")
          .SetOpenApi(Module.DatiContestazioniLabelEnti)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));


        endpointRouteBuilder
          .MapGet("api/notifiche/enti/contestazioni/reports", GetReportsContestazioniEnteAsync)
          .WithName("Permette visualizzare report steps attività contestazioni per ente  - Ente")
          .SetOpenApi(Module.DatiContestazioniLabelEnti)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion  
    }
} 