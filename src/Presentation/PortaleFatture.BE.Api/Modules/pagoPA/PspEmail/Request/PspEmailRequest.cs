namespace PortaleFatture.BE.Api.Modules.pagoPA.PspEmail.Request; 

public sealed class PspEmailRequest
{
    public string[]? ContractIds { get; set; } 
    public string[]? Quarters { get; set; }
    public string? Year { get; set; }
} 