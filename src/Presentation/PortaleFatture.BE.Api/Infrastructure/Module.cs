namespace PortaleFatture.BE.Api.Infrastructure;

public abstract class Module : IModule
{
    public static string DatiFatturazioneLabelPagoPA = "Dati Fatturazione PagoPA";
    public static string DatiFatturazioneLabel = "Dati Fatturazione";
    public static string DatiModuloCommessaLabelPagoPA = "Dati Modulo Commessa PagoPA";
    public static string DatiModuloCommessaLabel = "Dati Modulo Commessa";
    public static string DatiConfigurazioneModuloCommessaLabel = "Dati Configurazione Modulo Commessa";
    public static string DatiTipologiaLabel = "Dati Tipologie";
    public static string DatiNotificaLabel = "Dati Notifiche";
    public static string DatiFattureLabel = "Dati Fatture PagoPA";
    public static string DatiFattureEnti = "Dati Fatture Enti";
    public static string DatiRelLabel = "Dati Rel";
    public static string DatiRelLabelPagoPA = "Dati Rel PagoPA";
    public static string DatiNotificaLabelPagoPA = "Dati Notifiche PagoPA";
    public static string DatiNotificaLabelRecapitisti = "Dati Notifiche Recapitisti";
    public static string DatiNotificaLabelConsolidatori = "Dati Notifiche Consolidatori";
    public static string DatiAuthLabel = "Autenticazione";
    public static string DatiAsseverazioneLabelPagoPA = "Dati Asseverazione PagoPA";
    public static string DatiMessaggiPagoPA = "Dati Messaggi PagoPA";
    public static string DatiAccertamentiPagoPA = "Accertamenti PagoPA";
    public static string DatiOrchestratoreLabel = "Dati ORchestratore";
    public static string DatiApiKey = "Dati Api Key";

    // prodotto pagoPA
    public static string KPIPagamenti = "KPI Pagamenti prodotto PagoPA";
    public static string PSP = "PSP prodotto PagoPA";
    public static string FinancialReports = "Financial Reports prodotto PagoPA";




    public const string CORSLabel = "portalefatture";
    public const string GatewayLabel = "gateway";
    public const string HealthcheckLabel = "health";
    public const string LoggingLabel = "Logging";
    //
    public const string SelfCareRecapitistaPolicy = "SelfCareRecapitistaPolicy";
    public const string SelfCareConsolidatorePolicy = "SelfCareConsolidatorePolicy";
    public const string SelfCarePolicy = "SelfCarePolicy";
    public const string PagoPAPolicy = "PagoPAPolicy";
    //
    public const string SelfCarePolicyClaim = "auth";
    public const string SelfCarePolicyProfiloClaim = "profilo";
}