using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;
namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Dto;

public sealed class PSP
{
    [HeaderPagoPA(caption: "ContractId", Order = 1)]
    public string? ContractId { get; set; }

    [HeaderPagoPA(caption: "DocumentName", Order = 2)]
    public string? DocumentName { get; set; } 

    [HeaderPagoPA(caption: "ProviderNames", Order = 3)]
    public string? ProviderNames { get; set; }

    [HeaderPagoPA(caption: "SignedDate", Order = 4)]
    public DateTime? SignedDate { get; set; }

    [HeaderPagoPA(caption: "ContractType", Order = 5)]
    public string? ContractType { get; set; }

    [HeaderPagoPA(caption: "Name", Order = 6)]
    public string? Name { get; set; }

    [HeaderPagoPA(caption: "Abi", Order = 7)]
    public string? Abi { get; set; }

    [HeaderPagoPA(caption: "TaxCode", Order = 8)]
    public string? TaxCode { get; set; }

    [HeaderPagoPA(caption: "VatCode", Order = 9)]
    public string? VatCode { get; set; }

    [HeaderPagoPA(caption: "VatGroup", Order = 10)]
    public decimal? VatGroup { get; set; }

    [HeaderPagoPA(caption: "PecMail", Order = 11)]
    public string? PecMail { get; set; }

    [HeaderPagoPA(caption: "CourtesyMail", Order = 12)]
    public string? CourtesyMail { get; set; }

    [HeaderPagoPA(caption: "ReferenteFatturaMail", Order = 13)]
    public string? ReferenteFatturaMail { get; set; }

    [HeaderPagoPA(caption: "Sdd", Order = 14)]
    public string? Sdd { get; set; }

    [HeaderPagoPA(caption: "SdiCode", Order = 15)]
    public string? SdiCode { get; set; }

    [HeaderPagoPA(caption: "MembershipId", Order = 16)]
    public string? MembershipId { get; set; }

    [HeaderPagoPA(caption: "RecipientId", Order = 17)]
    public string? RecipientId { get; set; }

    [HeaderPagoPA(caption: "YearMonth", Order = 18)]
    public string? YearMonth { get; set; }

    [HeaderPagoPA(caption: "YearQuarter", Order = 19)]
    public string? YearQuarter { get; set; }
}
