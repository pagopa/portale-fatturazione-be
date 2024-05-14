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
    public static string DatiRelLabel = "Dati Rel";
    public static string DatiRelLabelPagoPA = "Dati Rel PagoPA";
    public static string DatiNotificaLabelPagoPA = "Dati Notifiche PagoPA";
    public static string DatiNotificaLabelRecapitisti = "Dati Notifiche Recapitisti";
    public static string DatiNotificaLabelConsolidatori = "Dati Notifiche Consolidatori";
    public static string DatiAuthLabel = "Autenticazione";
    public static string DatiAsseverazioneLabelPagoPA = "Dati Asseverazione PagoPA";

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