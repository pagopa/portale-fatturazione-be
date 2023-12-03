using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;


namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse;

public partial class DatiModuloCommessaModule : Module, IRegistrableModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    { 
        endpointRouteBuilder
         .MapPost("api/modulocommessa", CreateDatiModuloCommessaAsync)
         .WithName("Permette di creare/aggiornare i dati relativi al modulo commessa attuale.")
         .SetOpenApi(Module.DatiModuloCommessaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/modulocommessa", GetDatiModuloCommessaAsync)
         .WithName("Permette di ottenere i dati relativi al modulo commessa attuale.")
         .SetOpenApi(Module.DatiModuloCommessaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/modulocommessa/dettaglio/{anno}/{mese}", GetDatiModuloCommessaByAnnoMeseAsync)
         .WithName("Permette di ottenere i dati dettaglio relativi a un modulo commessa.")
         .SetOpenApi(Module.DatiModuloCommessaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/modulocommessa/lista/{anno?}", GetDatiModuloCommessaByAnnoAsync)
         .WithName("Permette di ottenere i dati relativi al modulo commessa per anno.")
         .SetOpenApi(Module.DatiModuloCommessaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/modulocommessa/lista/parziali/{anno?}", GetDatiModuloCommessaParzialiByAnnoAsync)
         .WithName("Permette di ottenere i dati parziali relativi al modulo commessa per anno.")
         .SetOpenApi(Module.DatiModuloCommessaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/modulocommessa/anni", GetDatiModuloCommessaAnniAsync)
         .WithName("Permette di ottenere gli anni relativi ai modulo commessa inseriti.")
         .SetOpenApi(Module.DatiModuloCommessaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/modulocommessa/documento/{anno?}/{mese?}", GetDatiModuloCommessaDocumentoAsync)
         .WithName("Permette di visualizzare il documento relativo a un modulo commessa")
         .SetOpenApi(Module.DatiModuloCommessaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/modulocommessa/download/{anno?}/{mese?}", DownloadDatiModuloCommessaDocumentoAsync)
         .WithName("Permette di scaricare il pdf relativo a un modulo commessa")
         .SetOpenApi(Module.DatiModuloCommessaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
}