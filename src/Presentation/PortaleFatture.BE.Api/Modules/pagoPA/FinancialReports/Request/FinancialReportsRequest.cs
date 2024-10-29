namespace PortaleFatture.BE.Api.Modules.pagoPA.FinancialReports.Request;

public class FinancialReportsRequest
{
    public string[]? ContractIds { get; set; }
    public string? MembershipId { get; set; }
    public string? RecipientId { get; set; }
    public string? ABI { get; set; }
    public string[]? Quarters { get; set; }
    public string? Year { get; set; }
}