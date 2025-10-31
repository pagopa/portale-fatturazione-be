namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

public class DatiModuloCommessaAderentiDto
{
    public DateTime DataExport { get; set; }
    public string? Internalistitutionid { get; set; }
    public string? Segmento { get; set; }
    public string? MacrocategoriaVendita { get; set; }
    public string? SottocategoriaVendita { get; set; }
    public string? Provincia { get; set; }
    public string? Regione { get; set; }
    public int TipoDistribuzione { get; set; }  // 1,2,3,4
}