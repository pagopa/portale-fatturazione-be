using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload.Response;

public class DatiModuloCommessaByAnnoResponse
{
    public bool Modifica { get; set; }
    public int AnnoValidita { get; set; }
    public int MeseValidita { get; set; }
    public string? IdEnte { get; set; }
    public long IdTipoContratto { get; set; }
    public string? Stato { get; set; }
    public string? Prodotto { get; set; }
    public string? Totale { get; set; } 
    public string? DataModifica { get; set; }
    public string? TotaleDigitale { get; set; }
    public string? TotaleAnalogico { get; set; }
}

public class ModuloCommessaMeseTotaleResponse
{
    public string? TotaleCategoria { get; set; }
    public int IdCategoriaSpedizione { get; set; }
    public string? Tipo { get; set; }
}