namespace PortaleFatture_BE_SendEmailFunction.Models.pagoPA;

public static class ConfigurazionepagoPA
{
    public static string? ConnectionString { get; set; }
    public static string? AccessToken { get; set; }
    public static string? RefreshToken { get; set; }
    public static string? ClientId { get; set; }
    public static string? ClientSecret { get; set; }
    public static string? From { get; set; }
    public static string? FromName { get; set; }

    public static string? To { get; set; }
    public static string? ToName { get; set; }
}