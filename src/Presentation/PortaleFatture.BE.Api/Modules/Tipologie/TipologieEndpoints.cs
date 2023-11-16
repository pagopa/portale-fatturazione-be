using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni;

public partial class TipologieModule : Module, IRegistrableModule
{ 
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
           .MapGet("api/tipologia/tipocontratto", GetAllTipoContrattoAsync)
           .WithName("Permette di ottenere i tipi contratti per la gestione dati commessa/fatturazione.")
           .SetOpenApi(Module.DatiTipologiaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/tipologia/tipocommessa", GetAllTipoCommessaAsync)
           .WithName("Permette di ottenere i tipi commessa per la gestione dati commessa/fatturazione.")
           .SetOpenApi(Module.DatiTipologiaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/tipologia/prodotto", GetAllProdottoAsync)
           .WithName("Permette di ottenere i tipi prodotti per la gestione dati configurazione modulo commessa.")
           .SetOpenApi(Module.DatiTipologiaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/tipologia/categoriaspedizione", GetAllCategoriaSpedizioneAsync)
           .WithName("Permette di ottenere le categoire spedizione per la gestione dati configurazione modulo commessa.")
           .SetOpenApi(Module.DatiTipologiaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
} 