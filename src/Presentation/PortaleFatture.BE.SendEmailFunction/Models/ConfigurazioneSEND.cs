﻿namespace PortaleFatture_BE_SendEmailFunction.Models;

public static class ConfigurazioneSEND
{
    public static string? ConnectionString { get; set; }
    public static string? From { get; set; }
    public static string? Smtp { get; set; }
    public static int SmtpPort { get; set; }
    public static string? SmtpAuth { get; set; }
    public static string? SmtpPassword { get; set; }
    public static string? StorageRELAccountName { get; set; }
    public static string? StorageRELAccountKey { get; set; }
    public static string? StorageRELBlobContainerName { get; set; }
}
 