using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.DatiConfigurazioneModuloCommesse;

public partial class DatiConfigurazioneModuloCommessaModule : Module, IRegistrableModule
{

    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {


        endpointRouteBuilder
           .MapGet("api/configurazionemodulocommessa", GetDatiConfigurazioneModuloCommessaAsync)
           .WithName("Permette di ottenere i dati relativi alla configurazione modulo commessa.")
           .SetOpenApi(Module.DatiConfigurazioneModuloCommessaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/configurazionemodulocommessa", CreateDatiConfigurazioneModuloCommessaAsync)
           .WithName("Permette di ottenere creare/aggiornare i dati relativi alla configurazione modulo commessa.")
           .SetOpenApi(Module.DatiConfigurazioneModuloCommessaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel)); 
    }
}
