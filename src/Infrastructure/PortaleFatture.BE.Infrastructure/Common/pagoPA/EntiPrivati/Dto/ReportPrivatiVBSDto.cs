namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.EntiPrivati.Dto;
public class ReportPrivatiVBSDto
{
    public string? RecipientId { get; set; }           // [recipient_id]
    public string? CodiceArticolo { get; set; }        // [CodiceArticolo]
    public int Numero { get; set; }                   // SUM([numero_tot])
    public decimal Valore { get; set; }               // SUM([valore_tot])
    public int TotaleAsync { get; set; }              // SUM([ASYNC_numero_tot])
    public decimal ValoreAsync { get; set; }          // SUM([ASYNC_valore_tot])
    public int TotaleSync { get; set; }               // SUM([SYNC_numero_tot])
    public decimal ValoreSync { get; set; }           // SUM([SYNC_valore_tot])
    public string? YearQuarter { get; set; }           // [year_quarter]
}