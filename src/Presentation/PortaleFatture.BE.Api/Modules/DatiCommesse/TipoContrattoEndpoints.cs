using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.DatiCommesse;

public partial class TipoContrattoModule : Module, IRegistrableModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
       .MapGet("api/datifatturazione/tipocontratto", GetTipoContrattoAsync)
       .WithName("Permette di ottenere i tipi contratti per la gestione dati commessa/fatturazione.")
       .SetOpenApi("Dati Commessa")
       .WithMetadata(new EnableCorsAttribute(policyName: "portalefatture"));
    }
} 