using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.AppIO.Contratti;

/// <summary>
/// Registrazione delle rotte dei contratti APP IO (PF-908, modello dati aggiornato con PF-1016).
/// </summary>
public partial class AppIoContrattiModule
{
    /// <summary>
    /// Registra le rotte api/appio/contracts*: filtri Tipo Ente e Nome Ente, griglia e download Excel.
    /// Il filtro "Nome Ente" serve anche la pagina dei documenti emessi.
    /// </summary>
    /// <param name="endpointRouteBuilder">Il builder delle rotte dell'applicazione.</param>
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
           .MapGet("api/appio/contracts/tipoente", GetAppIoContrattiTipiEnte)
           .WithName("Permette di visualizzare i tipi ente dei contratti AppIO.")
           .SetOpenApi(Module.DatiContrattiLabelAppIO)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/appio/contracts/name", PostAppIoContrattiByName)
           .WithName("Permette di ricercare gli enti AppIO per nome.")
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
