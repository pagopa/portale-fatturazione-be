using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure; 

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni;

partial class DatiFatturazioneModule : Module, IRegistrableModule
{ 
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
      endpointRouteBuilder
        .MapPost("api/datifatturazione", CreateDatiFatturazioneAsync)
        .WithName("Permette di salvare i dati relativi a una commessa/fatturazione.")
        .SetOpenApi(Module.DatiFatturazioneLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPut("api/datifatturazione", UpdateDatiFatturazioneAsync)
        .WithName("Permette di modificare i dati relativi a una commessa/fatturazione.")
        .SetOpenApi(Module.DatiFatturazioneLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapGet("api/datifatturazione/ente", GetDatiFatturazioneByIdEnteAsync)
        .WithName("Permette di recuperare il dato relativo alle commesse/fatturazione per id ente.")
        .SetOpenApi(Module.DatiFatturazioneLabel)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
} 