using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.Fatture;

public partial class FattureModule : Module, IRegistrableModule
{
    #region pagoPA
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
            .MapPost("api/fatture/pagopa/whitelist/download", PostPagoPAWhiteListFatturazioneDownloadAsync)
            .WithName("Permette di scaricare il file excel degli enti da non fatturare via utente PagoPA.")
            .SetOpenApi(Module.DatiFattureLabel)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapPost("api/fatture/pagopa/whitelist/inserisci", PostPagoPAWhiteListFatturazioneInserimentoAsync)
            .WithName("Permette di inserire gli enti non fatturabili in modifica via utente PagoPA.")
            .SetOpenApi(Module.DatiFattureLabel)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapPost("api/fatture/pagopa/whitelist/mesi/modifica", PostPagoPAWhiteListFatturazioneMesiModificaAsync)
            .WithName("Permette di recuperare i mesi relativi agli enti non fatturabili in modifica via utente PagoPA.")
            .SetOpenApi(Module.DatiFattureLabel)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapPost("api/fatture/pagopa/whitelist/anni/modifica", PostPagoPAWhiteListFatturazioneAnniModificaAsync)
            .WithName("Permette di recuperare gli anni relativi agli enti non fatturabili in modifica via utente PagoPA.")
            .SetOpenApi(Module.DatiFattureLabel)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapDelete("api/fatture/pagopa/whitelist", DeletePagoPAWhiteListFatturazioneAnniAsync)
            .WithName("Permette di eliminare le tipologie non fatturabili via utente PagoPA.")
            .SetOpenApi(Module.DatiFattureLabel)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapGet("api/fatture/pagopa/whitelist/tipologieFattura", GetPagoPAWhiteListFatturazioneTipologieAsync)
            .WithName("Permette di recuperare gli anni relativi alle tipologie non fatturabili via utente PagoPA.")
            .SetOpenApi(Module.DatiFattureLabel)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapGet("api/fatture/pagopa/whitelist/anni", GetPagoPAWhiteListFatturazioneAnniAsync)
            .WithName("Permette di recuperare gli anni relativi agli enti non fatturabili via utente PagoPA.")
            .SetOpenApi(Module.DatiFattureLabel)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapPost("api/fatture/pagopa/whitelist/mesi", PostPagoPAWhiteListFatturazioneMesiAsync)
            .WithName("Permette di recuperare i mesi relativi agli enti non fatturabili via utente PagoPA.")
            .SetOpenApi(Module.DatiFattureLabel)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapPost("api/fatture/pagopa/whitelist", PostPagoPAWhiteListFatturazioneAsync)
            .WithName("Permette di recuperare gli enti da non fatturare via utente PagoPA.")
            .SetOpenApi(Module.DatiFattureLabel)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapPost("api/fatture/contratti/download", PostContrattiTipologiaDownloadAsync)
            .WithName("Permette fare il download la lista contratti tipologia")
            .SetOpenApi(Module.DatiFattureLabel)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapPost("api/fatture/contratti/modifica", PostContrattiModificaAsync)
            .WithName("Permette di modificare un singolo contratto tipologia")
            .SetOpenApi(Module.DatiFattureLabel)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapPost("api/fatture/contratti/tipologia", PostContrattiTipologiaAsync)
            .WithName("Permette di visualizzare la lista contratti tipologia")
            .SetOpenApi(Module.DatiFattureLabel)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/resetta/pipeline", PostResettaFatturePipelineSapAsync)
           .WithName("Permette di resettare le fatture per invio a SAP")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));


        endpointRouteBuilder
           .MapPost("api/fatture/invio/pipeline", PostFatturePipelineSapAsync)
           .WithName("Permette di chiamare la pipeline per invio a SAP")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/invio/sap", PostFattureInvioSapAsync)
           .WithName("Permette di ottenere l'invio sap e numero fatture")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/fatture/invio/sap/multiplo", GetFattureInvioSapMultiploAsync)
           .WithName("Permette di ottenere l'invio sap multiplo con numero fatture ed Enti")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/invio/sap/multiplo/periodo", PostFattureInvioSapMultiploPeriodoAsync)
           .WithName("Permette di ottenere l'invio sap multiplo periodo anno mese tipologia")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPut("api/fatture/invio/sap/multiplo", PutFattureInvioSapMultiploPipelinePeriodoAsync)
           .WithName("Permette di inviare a sap multiplo lista anno mese tipologia")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));


        endpointRouteBuilder
           .MapGet("api/fatture/stato", GetStatoFattureAsync)
           .WithName("Permette di ottenere lo statp invio fatture per ricerca")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture", PostFattureByRicercaAsync)
           .WithName("Permette di ottenere le fatture per ricerca")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/fatture/tipologiacontratto", GetTipologiaContratto)
           .WithName("Permette di ottenere la tipologia contratto")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/download", PostFattureExcelByRicercaAsync)
           .WithName("Permette di ottenere le fatture excel per ricerca")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/report/prenotazione", PostFatturePrenotazioneReportByRicercaAsync)
           .WithName("Permette di ottenere la prenotazione fatture excel-report per ricerca")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/report", PostFattureReportByRicercaAsync)
           .WithName("Permette di ottenere le fatture excel-report per ricerca")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/tipologia", PostTipologiaFatture)
           .WithName("Permette di visualizzare la tipologia fatture")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/cancellazione", PostCancellazioneFatture)
           .WithName("Permette di cancellare o ripristinare le fatture non inviate")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapGet("api/fatture/anni", GetAnniFattureAsync)
          .WithName("Permette visualizzare anni fatture conta - pagoPA")
          .SetOpenApi(Module.DatiFattureLabel)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/fatture/mesi", PostMesiFattureAsync)
          .WithName("Permette visualizzare mesi fatture conta per anno - pagoPA")
          .SetOpenApi(Module.DatiFattureLabel)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/date", PostFattureDateByRicercaAsync)
           .WithName("Permette di ottenere le fatture date per ricerca")
           .SetOpenApi(Module.DatiFattureLabel)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        #endregion

        #region ente
        endpointRouteBuilder
        .MapPost("api/fatture/ente/tipologia", PostTipologiaEnteFatture)
            .WithName("Permette di visualizzare la tipologia fatture per ente")
            .SetOpenApi(Module.DatiFattureEnti)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapPost("api/fatture/ente", PostFattureEnteByRicercaAsync)
            .WithName("Permette di ottenere le fatture per ricerca singolo ente")
            .SetOpenApi(Module.DatiFattureEnti)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/ente/download", PostFattureEnteExcelByRicercaAsync)
           .WithName("Permette di ottenere le fatture excel per ricerca ente")
           .SetOpenApi(Module.DatiFattureEnti)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/fatture/ente/periodo", GetFattureEntePeriodoAsync)
           .WithName("Permette di ottenere mesi e anni delle fatture emesse lato ente")
           .SetOpenApi(Module.DatiFattureEnti)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/fatture/ente/periodo/sospeso", GetFattureEntePeriodoSospeseAsync)
           .WithName("Permette di ottenere mesi e anni delle fatture sospese lato ente")
           .SetOpenApi(Module.DatiFattureEnti)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapPost("api/fatture/ente/credito/sospeso", PostFattureCreditoSospesoRicercaAsync)
            .WithName("Permette di ottenere il credito sospeso e dettagli per ente")
            .SetOpenApi(Module.DatiFattureEnti)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapPost("api/fatture/ente/periodo/emesse", PostFattureEmesseRicercaAsync)
            .WithName("Permette di ottenere le fatture emesse e dettagli per ente")
            .SetOpenApi(Module.DatiFattureEnti)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapPost("api/fatture/ente/eliminate", PostFattureEliminateRicercaAsync)
            .WithName("Permette di ottenere le fatture eliminate e dettagli per ente")
            .SetOpenApi(Module.DatiFattureEnti)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
            .MapPost("api/fatture/ente/emesse/dettaglio", PostFattureEmesseDettaglioAsync)
            .WithName("Permette di ottenere il dettaglio di una specifica fattura emessa per ente")
            .SetOpenApi(Module.DatiFattureEnti)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
        

        endpointRouteBuilder
            .MapPost("api/fatture/ente/sospese/dettaglio", PostFattureSospeseDettaglioAsync)
            .WithName("Permette di ottenere il dettaglio di una specifica fattura sospesa per ente")
            .SetOpenApi(Module.DatiFattureEnti)
            .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));


        endpointRouteBuilder
           .MapPost("api/fatture/ente/emesse/download", PostFattureEmesseEnteExcelAsync)
           .WithName("Permette di scaricare l'excel delle fatture emesse per specifico ente")
           .SetOpenApi(Module.DatiFattureEnti)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/fatture/ente/sospese/download", PostFattureSospeseEnteExcelAsync)
           .WithName("Permette di scaricare l'excel delle fatture sospese per specifico ente")
           .SetOpenApi(Module.DatiFattureEnti)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

         endpointRouteBuilder
           .MapGet("api/fatture/ente/sospese/dettaglio/download/{id}", FattureSospeseDettaglioDownloadAsync)
           .WithName("Permette di scaricare il dettaglio in pdf della fatture sospesa per specifico ente")
           .SetOpenApi(Module.DatiFattureEnti)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

         endpointRouteBuilder
           .MapGet("api/fatture/ente/emesse/dettaglio/download/{id}", FattureEmesseDettaglioDownloadAsync)
           .WithName("Permette di scaricare il dettaglio in pdf della fatture emessa per specifico ente")
           .SetOpenApi(Module.DatiFattureEnti)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        #endregion
    }
}