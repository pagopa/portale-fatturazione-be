using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni;

public partial class TipologieModule : Module, IRegistrableModule
{ 
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
           .MapGet("api/tipologia/scadenziariocontestazioni", GetScadenziarioContestazioniByDescrizioneAsync)
           .WithName("Permette di visualizzare lo scadenziario contestazioni")
           .SetOpenApi(Module.DatiTipologiaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/tipologia/enti", AllEntiByDescrizioneAsync)
           .WithName("Permette di ottenere gli enti per ricerca descrizione e prodotto e profilo")
           .SetOpenApi(Module.DatiTipologiaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/tipologia/enti/completi", AllEntiCompletiByDescrizioneAsync)
           .WithName("Permette di ottenere gli enti per ricerca descrizione")
           .SetOpenApi(Module.DatiTipologiaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/tipologia/enti/consolidatore/completi", AllEntiCompletiConsolidatoreByDescrizioneAsync)
           .WithName("Permette di ottenere gli enti consolidatore per ricerca descrizione")
           .SetOpenApi(Module.DatiTipologiaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/tipologia/enti/fornitori", AllEntiCompletiFornitoriByTipoAsync)
           .WithName("Permette di ottenere gli enti fornitori per ricerca tipo")
           .SetOpenApi(Module.DatiTipologiaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/tipologia/tipoprofilo", GetAllTipoProfiloAsync)
           .WithName("Permette di ottenere i tipi profilo (institutionType) per la gestione dati commessa/fatturazione.")
           .SetOpenApi(Module.DatiTipologiaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

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
           .WithName("Permette di ottenere le categorie spedizione per la gestione dati configurazione modulo commessa.")
           .SetOpenApi(Module.DatiTipologiaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/tipologia/tipocontestazione", GetAllTipologiaContestazioniAsync)
           .WithName("Permette di ottenere le tipologie di contestazione per la gestione delle notifiche.")
           .SetOpenApi(Module.DatiTipologiaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/tipologia/flagcontestazione", GetAllFlagContestazioniAsync)
           .WithName("Permette di ottenere i flag di contestazione per la gestione delle notifiche.")
           .SetOpenApi(Module.DatiTipologiaLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
} 