namespace PortaleFatture.BE.Core.Auth;

public record ProfileInfo 
{
    public long? IdTipoContratto { get; set; } //nullable
    public string? Prodotto { get; set; }
    public string? IdEnte { get; set; }
    public string? Profilo { get; set; }
    public string? Email { get; set; }
    public string? Ruolo { get; set; }
    public string? DescrizioneRuolo { get; set; }
    public string? GruppoRuolo { get; set; }
    public string? NomeEnte { get; set; }
    public string? Id { get; set; }
    public string? Nonce { get; set; }
    public DateTime? Valido { get; set; }
    public string? JWT { get; set; } 
    public string? Auth { get; set; }
}