namespace PortaleFatture.BE.Api.Infrastructure;

public abstract class Module : IModule
{
    public static string DatiFatturazioneLabel = "Dati Fatturazione";
    public static string DatiModuloCommessaLabel = "Dati Modulo Commessa";
    public static string DatiConfigurazioneModuloCommessaLabel = "Dati Configurazione Modulo Commessa";
    public static string DatiTipologiaLabel = "Dati Tipologie";
    public static string DatiAuthLabel = "Autenticazione";
    public const string CORSLabel = "portalefatture";
    public const string GatewayLabel = "gateway";
    public const string HealthcheckLabel = "health";
    public const string LoggingLabel = "Logging";
}