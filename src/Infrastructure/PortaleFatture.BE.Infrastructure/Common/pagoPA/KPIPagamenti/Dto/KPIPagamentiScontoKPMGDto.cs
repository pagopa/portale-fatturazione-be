namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Dto;

public sealed class KPIPagamentiScontoKPMGDto
{
    public string? RecipientId { get; set; }

    public string? YearQuarter { get; set; }

    public decimal ValueTotal { get; set; }

    public decimal ValueDiscount { get; set; }

    public long TrxTotal { get; set; }
} 