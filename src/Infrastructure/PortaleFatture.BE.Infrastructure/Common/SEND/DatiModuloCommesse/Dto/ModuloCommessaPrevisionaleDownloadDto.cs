using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

public class ModuloCommessaPrevisionaleDownloadDto
{
    [Header(caption: "Anno", Order = 1)]
    public int Anno { get; set; }

    [Header(caption: "Mese", Order = 2)]
    public int Mese { get; set; }

    [Header(caption: "ID Ente", Order = 3)]
    public string? IdEnte { get; set; }

    [Header(caption: "Ente", Order = 4)]
    public string? Ente { get; set; }

    [Header(caption: "Tipo Report", Order = 5)]
    public string? TipoReport { get; set; }

    [Header(caption: "Totale Modulo Commessa", Order = 6)]
    public int? TotaleModuloCommessa { get; set; }

    [Header(caption: "AR", Order = 7)]
    public int? AR { get; set; }

    [Header(caption: "890", Order = 8)]
    public int? _890 { get; set; }

    [Header(caption: "Totale Regioni", Order = 9)]
    public int? TotaleRegioni { get; set; }

    [Header(caption: "Regione", Order = 10)]
    public string? Regione { get; set; }

    [Header(caption: "Calcolato", Order = 11)]
    public bool? Calcolato { get; set; }

    [Header(caption: "AR Regioni %", Order = 12)]
    public decimal? ArRegioniPerc { get; set; }

    [Header(caption: "890 Regioni %", Order = 13)]
    public decimal? Regioni890Perc { get; set; }

    [Header(caption: "Totale Regioni %", Order = 14)]
    public decimal? TotaleRegioniPerc { get; set; }

    [Header(caption: "Totale Copertura Regionale", Order = 15)]
    public string? TotaleCoperturaRegionale { get; set; }
}