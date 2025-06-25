
using CsvHelper.Configuration;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;


public sealed class ReportContestazioneStepsDtoMap : ClassMap<ReportContestazioneStepsWithLinkDto>
{
    public ReportContestazioneStepsDtoMap()
    {
        Map(m => m.ReportId).Name("report_id");
        Map(m => m.Step).Name("step");
        Map(m => m.DescrizioneStep).Name("descrizione_step"); 


        Map(m => m.TotaleNotificheAnalogicheARNazionaliAR).Name("notifiche_analogiche_ar_nazionali_ar");
        Map(m => m.TotaleNotificheAnalogicheARInternazionaliRIR).Name("notifiche_analogiche_ar_internazionali_rir");
 
        Map(m => m.TotaleNotificheAnalogicheRSNazionaliRS).Name("notifiche_analogiche_rs_nazionali_rs");
        Map(member => member.TotaleNotificheAnalogicheRSInternazionaliRIS).Name("notifiche_analogiche_rs_internazionali_ris");

        Map(m => m.TotaleNotificheAnalogiche890).Name("notifiche_analogiche_890");
        Map(m => m.TotaleNotificheDigitali).Name("notifiche_digitali");
        Map(m => m.TotaleNotifiche).Name("totale_notifiche");
        Map(m => m.NonContestataAnnullata).Name("non_contestata_annullata");
        Map(m => m.ContestataEnte).Name("contestata_ente");
        Map(m => m.RispostaEnte).Name("risposta_ente");
        Map(m => m.Accettata).Name("accettata");
        Map(m => m.RispostaSend).Name("risposta_send");
        Map(m => m.RispostaRecapitista).Name("risposta_recapitista");
        Map(m => m.RispostaConsolidatore).Name("risposta_consolidatore");
        Map(m => m.Rifiutata).Name("rifiutata");
        Map(m => m.NonFatturabile).Name("non_fatturabile");
        Map(m => m.Fatturabile).Name("fatturabile");
        Map(m => m.NomeDocumento).Name("nome_documento");
        Map(m => m.DataCompletamento).Name("data_completamento");
        Map(m => m.LinkDocumento).Name("link_documento");
    }
}

public sealed class SimpleNotificaEnteDtoMap : ClassMap<SimpleNotificaDto>
{
    public SimpleNotificaEnteDtoMap()
    {
        Map(m => m.IdContratto).Name("contract_id").Index(0);
        Map(m => m.CodiceFiscale).Name("tax_code").Index(1);
        Map(m => m.PIva).Name("vat_number").Index(2);
        Map(m => m.CAP).Name("zip_code").Index(3);
        Map(m => m.StatoEstero).Name("foreign_state").Index(4);
        Map(m => m.NumberOfPages).Name("number_of_pages").Index(5);
        Map(m => m.GEnvelopeWeight).Name("g_envelope_weight").Index(6);
        Map(m => m.CostEuroInCentesimi).Name("cost_eurocent").Index(7);
        Map(m => m.TimelineCategory).Name("timeline_category").Index(8);
        Map(m => m.TipoNotifica).Name("paper_product_type").Index(9);
        Map(m => m.NotificationType).Name("notification_type").Index(9);
        Map(m => m.IdNotifica).Name("event_id").Index(10);
        Map(m => m.IUN).Name("iun").Index(11);
        Map(m => m.Consolidatore).Name("consolidatore").Index(12);
        Map(m => m.Recapitista).Name("recapitista").Index(13);
        Map(m => m.DataInvio).Name("notification_sent_at").Index(14);
        Map(m => m.IdEnte).Name("internal_organization_id").Index(15);
        Map(m => m.RagioneSociale).Name("description").Index(16);
        Map(m => m.Data).Name("event_timestamp").Index(17);
        Map(m => m.RecipientIndex).Name("recipient_index").Index(18);
        Map(m => m.RecipientType).Name("recipient_type").Index(19);
        Map(m => m.RecipientId).Name("recipient_id").Index(20);
        Map(m => m.Anno).Name("year").Index(21);
        Map(m => m.Mese).Name("month").Index(22);
        Map(m => m.AnnoMeseGiorno).Name("daily").Index(23);
        Map(m => m.ItemCode).Name("item_code").Index(24);
        Map(m => m.NotificationRequestId).Name("notification_request_id").Index(25);
        Map(m => m.RecipientTaxId).Name("recipient_tax_id").Index(26);
        Map(m => m.NoteEnte).Name("Note Ente").Index(27);
        Map(m => m.RispostaEnte).Name("Risposta Ente").Index(28);
        Map(m => m.NoteSend).Name("Risposta Send").Index(29);
        Map(m => m.NoteRecapitista).Name("Risposta Recapitista").Index(30);
        Map(m => m.NoteConsolidatore).Name("Risposta Consolidatore").Index(31);
        Map(m => m.TipoContestazione).Name("Tipo Contestazione").Index(32);
        Map(m => m.IdTipoContestazione).Name("Id Tipo Contestazione").Index(32);
        Map(m => m.Contestazione).Name("Contestazione").Index(33);
        Map(m => m.StatoContestazione).Name("Stato contestazione").Index(34);
        Map(m => m.TipologiaFattura).Name("Tipologia Fattura").Index(35);
        //Map(m => m.CodiceOggetto).Name("Codice Oggetto").Index(36);
    }
}

public class SimpleNotificaPagoPADtoMap : ClassMap<SimpleNotificaDto>
{
    public SimpleNotificaPagoPADtoMap()
    {
        Map(m => m.IdContratto).Name("contract_id").Index(0);
        Map(m => m.CodiceFiscale).Name("tax_code").Index(1);
        Map(m => m.PIva).Name("vat_number").Index(2);
        Map(m => m.CAP).Name("zip_code").Index(3);
        Map(m => m.StatoEstero).Name("foreign_state").Index(4);
        Map(m => m.NumberOfPages).Name("number_of_pages").Index(5);
        Map(m => m.GEnvelopeWeight).Name("g_envelope_weight").Index(6);
        Map(m => m.CostEuroInCentesimi).Name("cost_eurocent").Index(7);
        Map(m => m.TimelineCategory).Name("timeline_category").Index(8);
        Map(m => m.TipoNotifica).Name("paper_product_type").Index(9);
        Map(m => m.NotificationType).Name("notification_type").Index(9);
        Map(m => m.IdNotifica).Name("event_id").Index(10);
        Map(m => m.IUN).Name("iun").Index(11);
        Map(m => m.Consolidatore).Name("consolidatore").Index(12);
        Map(m => m.Recapitista).Name("recapitista").Index(13);
        Map(m => m.DataInvio).Name("notification_sent_at").Index(14);
        Map(m => m.IdEnte).Name("internal_organization_id").Index(15);
        Map(m => m.RagioneSociale).Name("description").Index(16);
        Map(m => m.Data).Name("event_timestamp").Index(17);
        Map(m => m.RecipientIndex).Name("recipient_index").Index(18);
        Map(m => m.RecipientType).Name("recipient_type").Index(19);
        Map(m => m.RecipientId).Name("recipient_id").Index(20);
        Map(m => m.Anno).Name("year").Index(21);
        Map(m => m.Mese).Name("month").Index(22);
        Map(m => m.AnnoMeseGiorno).Name("daily").Index(23);
        Map(m => m.ItemCode).Name("item_code").Index(24);
        Map(m => m.NotificationRequestId).Name("notification_request_id").Index(25);
        Map(m => m.RecipientTaxId).Name("recipient_tax_id").Index(26);
        Map(m => m.NoteEnte).Name("Note Ente").Index(27);
        Map(m => m.RispostaEnte).Name("Risposta Ente").Index(28);
        Map(m => m.NoteSend).Name("Risposta Send").Index(29);
        Map(m => m.NoteRecapitista).Name("Risposta Recapitista").Index(30);
        Map(m => m.NoteConsolidatore).Name("Risposta Consolidatore").Index(31);
        Map(m => m.TipoContestazione).Name("Tipo Contestazione").Index(32);
        Map(m => m.IdTipoContestazione).Name("Id Tipo Contestazione").Index(32);
        Map(m => m.Contestazione).Name("Contestazione").Index(33);
        Map(m => m.StatoContestazione).Name("Stato contestazione").Index(34);
        Map(m => m.TipologiaFattura).Name("Tipologia Fattura").Index(35); 
        Map(m => m.CodiceOggetto).Name("Codice Oggetto").Index(36);
    }
}

public class RigheRelDtoEnteMap : ClassMap<RigheRelDto>
{
    public RigheRelDtoEnteMap()
    {
        Map(m => m.IdContratto).Name("contract_id").Index(0);
        Map(m => m.CodiceFiscale).Name("tax_code").Index(1);
        Map(m => m.PIva).Name("vat_number").Index(2);
        Map(m => m.CAP).Name("zip_code").Index(3);
        Map(m => m.StatoEstero).Name("foreign_state").Index(4);
        Map(m => m.Cost).Name("costo €").Index(5);
        Map(m => m.TimelineCategory).Name("timeline_category").Index(6);
        Map(m => m.TipoNotifica).Name("paper_product_type").Index(7);
        Map(m => m.IdNotifica).Name("event_id").Index(8);
        Map(m => m.IUN).Name("iun").Index(9);
        Map(m => m.Recapitista).Name("recapitista").Index(10);
        Map(m => m.DataInvio).Name("notification_sent_at").Index(11);
        Map(m => m.Data).Name("event_timestamp").Index(12);
        Map(m => m.Anno).Name("year").Index(13);
        Map(m => m.Mese).Name("month").Index(14);
        Map(m => m.AnnoMeseGiorno).Name("daily").Index(15);
    }
}

public class RigheRelDtoPagoPAMap : ClassMap<RigheRelDto>
{
    public RigheRelDtoPagoPAMap()
    {
        Map(m => m.IdContratto).Name("contract_id").Index(0);
        Map(m => m.CodiceFiscale).Name("tax_code").Index(1);
        Map(m => m.PIva).Name("vat_number").Index(2);
        Map(m => m.CAP).Name("zip_code").Index(3);
        Map(m => m.StatoEstero).Name("foreign_state").Index(4);
        Map(m => m.NumberOfPages).Name("number_of_pages").Index(5);
        Map(m => m.GEnvelopeWeight).Name("g_envelope_weight").Index(6);
        Map(m => m.Cost).Name("costo €").Index(7);
        Map(m => m.TimelineCategory).Name("timeline_category").Index(8);
        Map(m => m.TipoNotifica).Name("paper_product_type").Index(9);
        Map(m => m.IdNotifica).Name("event_id").Index(10);
        Map(m => m.IUN).Name("iun").Index(11);
        Map(m => m.Recapitista).Name("recapitista").Index(13);
        Map(m => m.DataInvio).Name("notification_sent_at").Index(14);
        Map(m => m.IdEnte).Name("internal_organization_id").Index(15);
        Map(m => m.Data).Name("event_timestamp").Index(16);
        Map(m => m.RecipientIndex).Name("recipient_index").Index(17);
        Map(m => m.RecipientType).Name("recipient_type").Index(18);
        Map(m => m.RecipientId).Name("recipient_id").Index(19);
        Map(m => m.Anno).Name("year").Index(20);
        Map(m => m.Mese).Name("month").Index(21);
        Map(m => m.AnnoMeseGiorno).Name("daily").Index(22);
        Map(m => m.ItemCode).Name("item_code").Index(23);
        Map(m => m.NotificationRequestId).Name("notification_request_id").Index(24);
        Map(m => m.RecipientTaxId).Name("recipient_tax_id").Index(25);
        Map(m => m.TipologiaFattura).Name("Tipologia Fattura").Index(33);
    }
}