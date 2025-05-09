namespace PortaleFatture.BE.Function.API.Models;

public interface IConfigurazione
{
    string? AESKey { get; set; }
    string? ConnectionString { get; set; }
    string? CustomDomain { get; set; }
}