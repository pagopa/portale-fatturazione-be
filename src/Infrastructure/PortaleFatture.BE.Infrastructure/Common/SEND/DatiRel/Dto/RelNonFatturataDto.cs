using System.ComponentModel.DataAnnotations.Schema;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;

public class RelNonFatturataDto
{
    [Column("internal_organization_id")]
    [Header(caption: "IdEnte", Order = 1)]
    [HeaderAttributev2(caption: "IdEnte", Order = 1)]
    public string? IdEnte { get; set; }


    [Header(caption: "Ragione Sociale", Order = 2)]
    [HeaderAttributev2(caption: "Ragione Sociale", Order = 2)]

    [Column("description")]
    public string? RagioneSociale { get; set; }


    [Header(caption: "IdContratto", Order = 3)]
    [HeaderAttributev2(caption: "IdContratto", Order = 3)]

    [Column("contract_id")]
    public string? IdContratto { get; set; }


    [Header(caption: "Tipologia Fattura", Order = 6)]
    [HeaderAttributev2(caption: "Tipologia Fattura", Order = 6)]

    [Column("TipologiaFattura")]
    public string? TipologiaFattura { get; set; }

    [Header(caption: "Anno", Order = 7)]
    [HeaderAttributev2(caption: "Anno", Order = 7)]

    [Column("year")]
    public int? Anno { get; set; }


    [Header(caption: "Mese", Order = 8)]
    [HeaderAttributev2(caption: "Mese", Order = 8)]

    [Column("month")]
    public int? Mese { get; set; }



    [Header(caption: "Totale Imponibile €", Order = 9)]
    [HeaderAttributev2(caption: "Totale Imponibile €", Order = 9)]
    [Column("Totale")]
    public decimal Totale { get; set; }


    [Header(caption: "Totale Ivato €", Order = 10)]
    [HeaderAttributev2(caption: "Totale Ivato €", Order = 10)]

    [Column("TotaleIva")]
    public decimal TotaleIva { get; set; }

    [Header(caption: "Firmata", Order = 11)]
    [HeaderAttributev2(caption: "Firmata", Order = 11)]
    public string Firmata
    {
        get
        {
            return Caricata.MapRelTestata();
        }
    }

    [Column("Caricata")]
    public byte Caricata { get; set; }

    [Header(caption: "Data Caricamento", Order = 12)]
    [HeaderAttributev2(caption: "Data Caricamento", Order = 12)]
    [Column("Data")]
    public DateTime Data { get; set; }


    [Header(caption: "Tipo Contratto", Order = 4)]
    [HeaderAttributev2(caption: "Tipo Contratto", Order = 4)]
    [Column("TipoContratto")]
    public string? TipoContratto { get; set; }


    [Header(caption: "Category", Order = 5)]
    [HeaderAttributev2(caption: "Category", Order = 5)]
    [Column("Category")]
    public string? Category { get; set; }
}