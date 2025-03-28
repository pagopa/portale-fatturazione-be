using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.SEND.Orchestratore;

public partial class OrchestratoreModule : Module, IRegistrableModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        #region pagoPA
        endpointRouteBuilder
           .MapPost("api/orchestratore", PostListOrchestratoreAsync)
           .WithName("Permette di ottenere i dati relativi all'orchestratore by date")
           .SetOpenApi(Module.DatiOrchestratoreLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/orchestratore/stati", GetOrchestratoreStati)
           .WithName("Permette di ottenere gli stati relativi all'orchestratore")
           .SetOpenApi(Module.DatiOrchestratoreLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/orchestratore/download", PostDownloadListOrchestratoreAsync)
           .WithName("Permette fare il download dei dati relativi all'orchestratore by date")
           .SetOpenApi(Module.DatiOrchestratoreLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/orchestratore/tipologie", GetOrchestratoreTipologie)
           .WithName("Permette di ottenere le tipologie relative all'orchestratore")
           .SetOpenApi(Module.DatiOrchestratoreLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/orchestratore/fasi", GetOrchestratoreFasi)
           .WithName("Permette di ottenere le fasi relative all'orchestratore")
           .SetOpenApi(Module.DatiOrchestratoreLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion
    } 
}