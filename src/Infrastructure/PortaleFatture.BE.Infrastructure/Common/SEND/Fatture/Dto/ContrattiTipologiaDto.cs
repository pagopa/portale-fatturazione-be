using System.Text.Json.Serialization;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

public sealed class SingleContrattiTipologiaDto
{
    [HeaderAttributev2(caption: "IdEnte", Order = 2)]
    public string? IdEnte { get; set; }

    [HeaderAttributev2(caption: "Ragione Sociale", Order = 1)]
    public string? RagioneSociale { get; set; } 
 
    public DateTime UltimaModificaContratto { get; set; }
 
    public int TipoContratto { get; set; }

    [HeaderAttributev2(caption: "Tipo Contratto", Order = 4)]
    public string? DescrizioneTipoContratto { get; set; }


    [HeaderAttributev2(caption: "IdContratto", Order = 3)]
    public string? IdContratto { get; set; }

    [HeaderAttributev2(caption: "Data Aggiornamento", Order = 5)]
    public DateTime? DataInserimento { get; set; } 
}

public sealed class ContrattiTipologiaDto
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<SingleContrattiTipologiaDto>? Contratti { get; set; }
    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}
