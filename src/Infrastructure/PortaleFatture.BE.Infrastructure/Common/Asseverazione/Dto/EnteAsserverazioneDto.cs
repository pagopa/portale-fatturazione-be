using System.ComponentModel.DataAnnotations.Schema;
using PortaleFatture.BE.Infrastructure.Common.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.Asseverazione.Dto;

public class EnteAsserverazioneDto
{
    [Column("internalistitutionid")]
    [HeaderAttributev2(caption: "Id Ente", Order = 1)]
    public string? IdEnte { get; set; }

    [Column("description")]
    [HeaderAttributev2(caption: "Ragione Sociale", Order = 2)]
    public string? RagioneSociale { get; set; }

    [Column("product")]
    [HeaderAttributev2(caption: "Prodotto", Order = 3)]
    public string? Prodotto { get; set; }

    [Column("IdTipoContratto")]
    [HeaderAttributev2(caption: "Id Tipo Contratto", Order = 4)]
    public long? IdTipoContratto { get; set; }

    [Column("TipoContratto")]
    [HeaderAttributev2(caption: "TipoContratto", Order = 5)]
    public string? TipoContratto { get; set; }

    [Column("Asseverazione")]
    public bool? Asseverazione { get; set; }

    [Column("calcolo_asseverazione")]
    public bool? CalcoloAsseverazione { get; set; }

    [Column("data_asseverazione")]
    [HeaderAttributev2(caption: "Data Adesione al Bando", Order = 12)]
    public DateTime? DataAsseverazione { get; set; }

    [Column("data_anagrafica")]
    [HeaderAttributev2(caption: "Data Ultima Modifica Anagrafica", Order = 7)]
    public DateTime? DataAnagrafica { get; set; }

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

    [HeaderAttributev2(caption: "Calcolo Asseverazione", Order = 9)]
    public string? TipoCalcoloAsseverazione
    {
        get
        {
            if (CalcoloAsseverazione.HasValue)
            {
                if (CalcoloAsseverazione.Value)
                {
                    return "SI";
                }
                else
                {
                    return "NO";
                }
            }
            else
                return "NON CALCOLATO";
        }
    }

    [Column("timestamp")]
    [HeaderAttributev2(caption: "Timestamp", Order = 15)]
    public DateTime? Timestamp { get; set; }

    [Column("idutente")]
    [HeaderAttributev2(caption: "Id Utente", Order = 16)]
    public string? IdUtente { get; set; }

    [Column("Descrizione")]
    [HeaderAttributev2(caption: "Descrizione", Order = 14)]
    public string? Descrizione { get; set; }
} 