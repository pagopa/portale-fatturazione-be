namespace PortaleFatture.BE.Api.Modules.pagoPA.KPIPagamenti.Request;

public sealed class KPIPagamentiRequest
{
    public string[]? ContractIds { get; set; }
    public string? MembershipId { get; set; }
    public string? RecipientId { get; set; }
    public string? ProviderName { get; set; }
    public string[]? Quarters { get; set; }
    public string? Year { get; set; }
}