using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.Fatture;

public partial class AccertamentiModule : Module, IRegistrableModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
           .MapPost("api/accertamenti/report", PostReportAccertamentiByRicercaAsync)
           .WithName("Permette di ottenere i report accertamenti per ricerca")
           .SetOpenApi(Module.DatiAccertamentiPagoPA)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/accertamenti/report/prenotazione", PostPrenotazioneReportByRicercaAsync)
           .WithName("Permette di ottenere la prenotazione accertamenti excel-report per ricerca")
           .SetOpenApi(Module.DatiAccertamentiPagoPA)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/accertamenti/report/download", PostDownloadReportAccertamentiAsync)
           .WithName("Permette di ottenere il report specifico")
           .SetOpenApi(Module.DatiAccertamentiPagoPA)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/accertamenti/matrice/data", GetDataMatriceRecapitistiAsync)
           .WithName("Permette di ottenere le date di inizio della matrice costi recapitisti")
           .SetOpenApi(Module.DatiAccertamentiPagoPA)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/accertamenti/matrice", PostMatriceRecapitistiAsync)
           .WithName("Permette di ottenere il report excel della matrice costi recapitisti")
           .SetOpenApi(Module.DatiAccertamentiPagoPA)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
} 