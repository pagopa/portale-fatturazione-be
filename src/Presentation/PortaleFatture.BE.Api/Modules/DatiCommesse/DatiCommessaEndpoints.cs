using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure; 

namespace PortaleFatture.BE.Api.Modules.DatiCommesse;

partial class DatiCommessaModule : Module, IRegistrableModule
{ 
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
      endpointRouteBuilder
        .MapPost("api/datifatturazione", CreateDatiCommessaAsync)
        .WithName("Permette di salvare i dati relativi a una commessa/fatturazione.")
        .SetOpenApi("Dati Commessa")
        .WithMetadata(new EnableCorsAttribute(policyName: "portalefatture"));

        endpointRouteBuilder
        .MapPut("api/datifatturazione/{idente}", UpdateDatiCommessaAsync)
        .WithName("Permette di modificare i dati relativi a una commessa/fatturazione.")
        .SetOpenApi("Dati Commessa")
        .WithMetadata(new EnableCorsAttribute(policyName: "portalefatture"));

        endpointRouteBuilder
        .MapGet("api/datifatturazione/{id}", GetDatiCommessaByIdAsync)
        .WithName("Permette di recuperare i dati relativi a una commessa/fatturazione per id.")
        .SetOpenApi("Dati Commessa")
        .WithMetadata(new EnableCorsAttribute(policyName: "portalefatture"));

        endpointRouteBuilder
        .MapGet("api/datifatturazione/lista/{idente}", GetDatiCommessaAllByIdEnteAsync)
        .WithName("Permette di recuperare tutti i dati relativi alle commesse/fatturazione per id ente.")
        .SetOpenApi("Dati Commessa")
        .WithMetadata(new EnableCorsAttribute(policyName: "portalefatture"));
    }
} 