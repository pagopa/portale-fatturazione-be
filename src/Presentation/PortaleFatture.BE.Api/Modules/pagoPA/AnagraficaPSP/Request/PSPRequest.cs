namespace PortaleFatture.BE.Api.Modules.pagoPA.AnagraficaPSP.Request;

public class PSPRequest
{ 
    public string[]? ContractIds { get; set; }
    public string? MembershipId { get; set; }
    public string? RecipientId { get; set; } 
    public string? ABI { get; set; }

}