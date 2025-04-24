using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.SEND.APIKey;


partial class ApiKeyModule : Module, IRegistrableModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
           .MapGet("api/apikey", GetApiKeys)
           .WithName("Permette di visualizzare le api key laddove applicabile")
           .SetOpenApi(Module.DatiApiKey)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        endpointRouteBuilder
           .MapPost("api/apikey/genera", PostCreateApiKey)
           .WithName("Permette di generale una api key")
           .SetOpenApi(Module.DatiApiKey)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        endpointRouteBuilder
           .MapGet("api/apikey/ips", GetApiKeysIps)
           .WithName("Permette di visualizzare gli ip associati alle apikey")
           .SetOpenApi(Module.DatiApiKey)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        endpointRouteBuilder
           .MapPost("api/apikey/ips", PostApiKeysIps)
           .WithName("Permette di aggiungere gli ip associati alle apikey")
           .SetOpenApi(Module.DatiApiKey)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        endpointRouteBuilder
           .MapDelete("api/apikey/ips", DeleteApiKeysIps)
           .WithName("Permette di eliminare gli ip associati alle apikey")
           .SetOpenApi(Module.DatiApiKey)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
}