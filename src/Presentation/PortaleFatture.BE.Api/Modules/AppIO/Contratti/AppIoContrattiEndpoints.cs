using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.AppIO.Contratti;

/// <summary>
/// Registrazione delle rotte dei contratti APP IO (PF-908).
/// </summary>
public partial class AppIoContrattiModule
{
    /// <summary>
    /// Registra le rotte api/appio/contracts*: filtri Anno, Trimestre e Nome Ente, griglia e
    /// download Excel.
    /// </summary>
    /// <param name="endpointRouteBuilder">Il builder delle rotte dell'applicazione.</param>
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
           .MapGet("api/appio/contracts/years", GetAppIoContrattiYears)
           .WithName("Permette di visualizzare gli anni in cui sono presenti contratti AppIO.")
           .SetOpenApi(Module.DatiContrattiLabelAppIO)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/appio/contracts/quarters", PostAppIoContrattiQuarters)
           .WithName("Permette di visualizzare i trimestri dei contratti AppIO per anno.")
           .SetOpenApi(Module.DatiContrattiLabelAppIO)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/appio/contracts/name", PostAppIoContrattiByName)
           .WithName("Permette di ricercare i contratti AppIO per nome dell'ente.")
           .SetOpenApi(Module.DatiContrattiLabelAppIO)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/appio/contracts", PostAppIoContratti)
           .WithName("Permette di visualizzare i contratti AppIO.")
           .SetOpenApi(Module.DatiContrattiLabelAppIO)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/appio/contracts/download", PostAppIoContrattiDownload)
           .WithName("Permette di scaricare il file excel dei contratti AppIO.")
           .SetOpenApi(Module.DatiContrattiLabelAppIO)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
}
