using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.Fatture;

public partial class FattureModule : Module, IRegistrableModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
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
           .MapPost("api/fatture/tipologia", PostTipologiaFatture)
           .WithName("Permette di visualizzare la tipologia fatture")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel)); 
    }
} 