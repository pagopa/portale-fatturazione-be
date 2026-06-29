using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

public class SimpleFattureDaNonInviareSapDto
{
    public string Id { get; set; }

    [HeaderAttributev2(caption: "Ragione Sociale", Order = 1)]
    public string? RagioneSociale { get; set; }

    [HeaderAttributev2(caption: "IdEnte", Order = 2)]
    public string? IdEnte { get; set; }

    [HeaderAttributev2(caption: "Anno", Order = 3)]
    public int Anno { get; set; }

    public int Mese { get; set; }

    [HeaderAttributev2(caption: "Mese", Order = 4)]
    public string MeseDescrizione
    {
        get
        {
            return Mese.GetMonth();
        }
    }

    [HeaderAttributev2(caption: "Data Inizio", Order = 5)]
    public DateTime? DataInserimento { get; set; }
    public DateTime? DataCancellazione { get; set; }
    [HeaderAttributev2(caption: "Data Cancellazione", Order = 6)]
    public DateTime? DataRipristino { get; set; }
    [HeaderAttributev2(caption: "Data Ripristino", Order = 7)]
    public string? TipologiaFattura { get; set; }
    public int IdTipoContratto { get; set; }
    [HeaderAttributev2(caption: "Tipo Contratto", Order = 8)]
    public string? TipoContratto { get; set; }

    public int Stato { get; set; }

}

public sealed class FattureDaNonInviareSapDto
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<SimpleFattureDaNonInviareSapDto>? FattureEscluse { get; set; }
    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}
