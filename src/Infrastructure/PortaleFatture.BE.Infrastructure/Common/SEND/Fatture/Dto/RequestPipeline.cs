using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
public class PipelineParameter()
{
    [JsonPropertyName("tipoFattura")]
    public string? TipoFattura { get; set; }

    [JsonPropertyName("MesidaFatturare")]
    public List<string>? MesiDaFatturare { get; set; }
}
 
public class ParametriFatturazioneInvioSAP
{
    [JsonPropertyName("parametri")]
    public List<PipelineParameter>? Parametri { get; set; }
}