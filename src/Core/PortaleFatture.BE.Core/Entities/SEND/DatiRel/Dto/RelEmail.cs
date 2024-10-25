namespace PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;

public class RelEmail
{
    public string? IdEnte { get; set; }
    public string? IdContratto { get; set; }
    public string? TipologiaFattura { get; set; }
    public int Anno { get; set; }
    public int Mese { get; set; }
    public string? Pec { get; set; }
    public string? RagioneSociale { get; set; }
}

public sealed class RelEmailTracking : RelEmail
{
    public string? Messaggio { get; set; }
    public string? Data { get; set; }
    public byte Invio { get; set; }
}