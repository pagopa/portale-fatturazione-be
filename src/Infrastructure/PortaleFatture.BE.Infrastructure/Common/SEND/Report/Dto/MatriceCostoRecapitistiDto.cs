using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Report.Dto;

public class MatriceCostoRecapitistiDto
{
    [HeaderAttributev2(caption: "Geokey", Order = 1)]
    public string? Geokey { get; set; }


    [HeaderAttributev2(caption: "Foreign State", Order = 2)]
    public string? ForeignState { get; set; }


    [HeaderAttributev2(caption: "Product", Order = 3)]
    public string? Product { get; set; }


    [HeaderAttributev2(caption: "Recapitista", Order = 4)]
    public string? Recapitista { get; set; }


    [HeaderAttributev2(caption: "Lotto", Order = 5)]
    public int? Lotto { get; set; }

    [HeaderAttributev2(caption: "Costo Plico", Order = 6)]
    public int? CostoPlico { get; set; }

    [HeaderAttributev2(caption: "Costo Foglio", Order = 7)]
    public int? CostoFoglio { get; set; }

    [HeaderAttributev2(caption: "Costo Demat", Order = 8)]
    public int? CostoDemat { get; set; }

    [HeaderAttributev2(caption: "Min", Order = 9)]
    public int? Min { get; set; }

    [HeaderAttributev2(caption: "Max", Order = 10)]
    public int? Max { get; set; }

    [HeaderAttributev2(caption: "Costo", Order = 11)]
    public int? Costo { get; set; }

    [HeaderAttributev2(caption: "Costo Base 20Gr", Order = 12)]
    public int? CostoBase20Gr { get; set; }

    [HeaderAttributev2(caption: "Id Recapitista", Order = 13)]
    public string? IdRecapitista { get; set; }

    [HeaderAttributev2(caption: "Data Inizio Validita", Order = 14)]
    public DateTime? DataInizioValidita { get; set; }

    [HeaderAttributev2(caption: "Data Fine Validita", Order = 15)]
    public DateTime? DataFineValidita { get; set; }
}
