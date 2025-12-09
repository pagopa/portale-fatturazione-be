using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;


namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse;

public partial class DatiModuloCommessaModule : Module, IRegistrableModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        #region PagoPA 
        endpointRouteBuilder
         .MapGet("api/modulocommessa/pagopa/tipologiacontratto", GetTipologiaContratto)
         .WithName("Permette di ottenere la tipologia contratto REL in modulo commessa")
         .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapGet("api/modulocommessa/pagopa/periodo", GetPagoPADatiModuloCommessaAnnoMeseAsync)
        .WithName("Permette di ottenere i dati relativi al modulo commessa via utente PagoPA anno mese.")
        .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/modulocommessa/pagopa/", GetPagoPADatiModuloCommessaAsync)
         .WithName("Permette di ottenere i dati relativi al modulo commessa via utente PagoPA per mese anno.")
         .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/modulocommessa/pagopa/documento/ricerca", PagoPADatiModuloCommessaRicercaDocumentAsync)
        .WithName("Permette di ottenere il file excel per il modulo commessa per ricerca descrizione via utente PagoPA")
        .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/modulocommessa/pagopa/ricerca", PagoPADatiModuloCommessaRicercaAsync)
        .WithName("Permette di ottenere i dati relativi al modulo commessa via utente PagoPA.")
        .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/v2/modulocommessa/pagopa/", GetPagoPADatiModuloCommessav2Async)
         .WithName("Permette di ottenere i dati relativi al modulo commessa via utente PagoPA per mese anno v2.")
         .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel)); 

        endpointRouteBuilder
          .MapGet("api/v2/modulocommessa/pagopa/regioni", GetDatiModuloCommessaListaObbligatoriPagoPAAsync)
          .WithName("Permette di ottenere lista regioni/province per inserimento modulo commessa pagoPA v2.")
          .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/v2/modulocommessa/pagopa/documento/{anno?}/{mese?}", GetDatiModuloCommessaDocumentoPagoPAAsync)
         .WithName("Permette di visualizzare il documento relativo a un modulo commessa previsionale pagoPA v2")
         .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/v2/modulocommessa/pagopa/ricerca", PagoPADatiModuloCommessaRicercaAsync)
        .WithName("Permette di ottenere i dati relativi al modulo commessa via utente PagoPA v2.")
        .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/v2/modulocommessa/pagopa/report/ricerca", PagoPADatiModuloCommessaRicercaDocumentoReportAsync)
        .WithName("Permette di ottenere il file excel per il modulo commessa report per ricerca descrizione via utente PagoPA")
        .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));


        endpointRouteBuilder
        .MapPost("api/v2/modulocommessa/pagopa/ricerca/date", PagoPADatiModuloCommessaRicercaByDateAsync)
        .WithName("Permette di ottenere i dati relativi al modulo commessa via utente PagoPA v2 con date")
        .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
        .MapPost("api/v2/modulocommessa/pagopa/report/ricerca/date", PagoPADatiModuloCommessaRicercaDocumentoReportByDateAsync)
        .WithName("Permette di ottenere il file excel per il modulo commessa report per ricerca descrizione via utente PagoPA con date")
        .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
        .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder 
          .MapPost("api/v2/pagopa/modulocommessa", CreateDatiModuloCommessaPrevisionalePagoPAAsync)
          .WithName("Permette di creare/aggiornare i dati relativi al modulo commessa previsionale pagoPA")
          .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/modulocommessa/pagopa", CreatePagoPADatiModuloCommessaAsync)
           .WithName("Permette di creare/aggiornare i dati relativi al modulo commessa attuale via utente PagoPA.")
           .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/modulocommessa/pagopa/dettaglio/{anno}/{mese}", GetPagoPADatiModuloCommessaByAnnoMeseAsync)
         .WithName("Permette di ottenere i dati dettaglio relativi a un modulo commessa via utente PagoPA.")
         .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/v2/modulocommessa/pagopa/dettaglio/{anno}/{mese}", GetPagoPADatiModuloCommessaByAnnoMesev2Async)
         .WithName("Permette di ottenere i dati dettaglio relativi a un modulo commessa via utente PagoPA v2.")
         .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/modulocommessa/pagopa/documento/{anno?}/{mese?}", GetPagoPADatiModuloCommessaDocumentoAsync)
         .WithName("Permette di visualizzare il documento relativo a un modulo commessa via utente PagoPA.")
         .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/modulocommessa/pagopa/download/{anno?}/{mese?}", DownloadPagoPADatiModuloCommessaDocumentoAsync)
         .WithName("Permette di scaricare il pdf relativo a un modulo commessa via utente PagoPA.")
         .SetOpenApi(Module.DatiModuloCommessaLabelPagoPA)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion

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
         .MapGet("api/v2/modulocommessa/lista/{anno?}", GetDatiModuloCommessaPrevisionaleByAnnoAsync)
         .WithName("Permette di ottenere i dati previsionali relativi al modulo commessa per anno e source.")
         .SetOpenApi(Module.DatiModuloCommessaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/v2/modulocommessa/obbligatori/verifica", GetDatiModuloCommessaOkObbligatoriAsync)
         .WithName("Permette di ottenere ok/ko per inserimento obbligatori modulo commessa.")
         .SetOpenApi(Module.DatiModuloCommessaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/v2/modulocommessa/obbligatori/lista", GetDatiModuloCommessaRegioniAsync)
         .WithName("Permette di ottenere lista per inserimento obbligatori modulo commessa.")
         .SetOpenApi(Module.DatiModuloCommessaLabel)
         .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapGet("api/v2/modulocommessa/regioni", GetDatiModuloCommessaListaObbligatoriAsync)
          .WithName("Permette di ottenere lista regioni/province per inserimento modulo commessa.")
          .SetOpenApi(Module.DatiModuloCommessaLabel)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapGet("api/v2/modulocommessa/dettaglio/{anno}/{mese}", GetDatiModuloCommessaByAnnoMesePrevisionaleAsync)
          .WithName("Permette di ottenere i dati dettaglio relativi a un modulo commessa previsionale.")
          .SetOpenApi(Module.DatiModuloCommessaLabel)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/v2/modulocommessa", CreateDatiModuloCommessaPrevisionaleAsync)
          .WithName("Permette di creare/aggiornare i dati relativi al modulo commessa previsionale.")
          .SetOpenApi(Module.DatiModuloCommessaLabel)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
         .MapGet("api/v2/modulocommessa/documento/{anno?}/{mese?}", GetDatiModuloCommessaDocumentoAsync)
         .WithName("Permette di visualizzare il documento relativo a un modulo commessa previsionale")
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