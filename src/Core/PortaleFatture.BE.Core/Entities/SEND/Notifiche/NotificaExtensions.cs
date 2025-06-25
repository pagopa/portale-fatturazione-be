namespace PortaleFatture.BE.Core.Entities.SEND.Notifiche;

public static class NotificaExtensions
{
    public static string? Map(this TipoNotifica? tipo)
    {
        return tipo switch
        {
            TipoNotifica.Digitali => string.Empty,
            TipoNotifica.AnalogicoARNazionali => "AR",
            TipoNotifica.AnalogicoARInternazionali => "RIR",
            TipoNotifica.AnalogicoRSNazionali => "RS",
            TipoNotifica.AnalogicoRSInternazionali => "RIS",
            TipoNotifica.Analogico890 => "890",
            _ => null
        };
    }

    public static string? Map(this string? dbValue)
    {
        return dbValue switch
        {
            "AR" => TipoNotifica.AnalogicoARNazionali.ToString(),
            "RIR" => TipoNotifica.AnalogicoARInternazionali.ToString(),
            "RS" => TipoNotifica.AnalogicoRSNazionali.ToString(),
            "RIS" => TipoNotifica.AnalogicoRSInternazionali.ToString(),
            "890" => TipoNotifica.Analogico890.ToString(),
            _ => TipoNotifica.Digitali.ToString(),
        };
    } 
}