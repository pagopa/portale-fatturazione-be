using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.Asseverazione;

public partial class AsseverazioneModule : Module, IRegistrableModule
{ 
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
        .MapPost("api/asseverazione", PostAsseverazioneAsync)
        .WithName("Permette di visualizzare il log asseverazione")
        .SetOpenApi(Module.DatiAsseverazioneLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/asseverazione/documento", PostAsseverazioneDocumentoAsync)
        .WithName("Permette di visualizzare il documento log asseverazione")
        .SetOpenApi(Module.DatiAsseverazioneLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/asseverazione/export", PostAsseverazioneExportDocumentoAsync)
        .WithName("Permette di visualizzare il documento export asseverazione")
        .SetOpenApi(Module.DatiAsseverazioneLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));


        endpointRouteBuilder
        .MapPost("api/asseverazione/upload/", PostUploadAsync)
        .WithName("Permette caricare il file import excel")
        .SetOpenApi(Module.DatiAsseverazioneLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
} 