namespace PortaleFatture.BE.Core.Extensions;

public static class MimeMapping
{
    public static string PDF = "application/pdf";
    public static string ZIP = "application/zip";

    public static readonly Dictionary<string, string> Extensions = new(StringComparer.InvariantCultureIgnoreCase)
    {
        { "text/plain", ".txt" },
        { "text/html", ".html" },
        { "text/css", ".css" },
        { "text/javascript", ".js" },
        { "application/json", ".json" },
        { "application/xml", ".xml" },
        { "image/jpeg", ".jpg" },
        { "image/png", ".png" },
        { "image/gif", ".gif" },
        { "image/bmp", ".bmp" },
        { "application/pdf", ".pdf" },
        { "application/zip", ".zip" },
        { "text/csv", ".csv" },
        { "application/vnd.ms-excel", ".xlsx" }
    };
}

public static class LanguageMapping
{
    public static string IT = "it-IT";
} 