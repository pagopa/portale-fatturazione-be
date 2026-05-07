namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

public class QueryDto
{
    public string? IdEnte { get; set; } = null;
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public int? Page { get; set; }
    public int? Size { get; set; }
    public string? Prodotto { get; set; }

    public string? Cap { get; set; }
    public string? Profilo { get; set; }
    public string? TipoNotifica { get; set; }
    public int[]? Contestazione { get; set; }
    public string? Iun { get; set; }
    public string[]? EntiIds { get; set; } = null;
    public string? RecipientId { get; set; }
    public string? Recapitista { get; set; }
    public string? Consolidatore { get; set; }
    public string[]? Recapitisti { get; set; }
    public string[]? Consolidatori { get; set; }
    public string? Order { get; set; }   // sarà la stringa che ci dirà asc desc o null
    public string? ColumnName { get; set; }  // nome della colonna lato fe dove verra applicato l'if lato be
}