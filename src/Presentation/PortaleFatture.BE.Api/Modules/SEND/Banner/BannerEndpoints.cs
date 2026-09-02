using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.Banner;

public partial class BannerModule : Module, IRegistrableModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
        .MapGet("api/info-banner", GetBannerAsync)
        .WithName("Permette di visualizzare il banner più recente e attivo")
        .SetOpenApi(Module.InfoBanner)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

    }
}