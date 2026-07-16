using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.LanguageService;

public partial class LanguageService : Module, IRegistrableModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {

        endpointRouteBuilder
        .MapPost("api/piid", PostPiidAsync)
        .WithName("Permette di inviare una stringa che verrà redatta oscurando le Personal Identifiable Information (PII)")
        .SetOpenApi(Module.LanguageService)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

    }
}