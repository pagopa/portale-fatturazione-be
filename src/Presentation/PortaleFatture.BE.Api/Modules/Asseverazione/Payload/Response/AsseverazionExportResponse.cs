using PortaleFatture.BE.Infrastructure.Common.Documenti.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Api.Modules.Asseverazione.Payload.Response;

public class AsseverazionExportResponse
{
    [Column("internalistitutionid")]
    [HeaderAttributev2(caption: "Id Ente", Order = 1)]
    public string? IdEnte { get; set; }

    [Column("description")]
    [HeaderAttributev2(caption: "Ragione Sociale", Order = 2)]
    public string? RagioneSociale { get; set; } 

    [Column("Asseverazione")]
    public bool? Asseverazione { get; set; } 

    [Column("data_asseverazione")]
    [HeaderAttributev2(caption: "Data Adesione al Bando", Order = 12)]
    public DateTime? DataAsseverazione { get; set; } 

    [HeaderAttributev2(caption: "Adessione al Bando", Order = 13)]
    public string? TipoAsseverazione
    {
        get
        {
            if (Asseverazione.HasValue)
            {
                if (Asseverazione.Value)
                {
                    return "SI";
                }
                else
                {
                    return "NO";
                }
            }
            else
                return string.Empty;
        }
    } 
    [HeaderAttributev2(caption: "Descrizione", Order = 14)]
    public string? Descrizione { get; set; }
} 