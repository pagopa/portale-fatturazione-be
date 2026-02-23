using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using System.Text.Json.Serialization;
namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;

public class NotificheRicercaRequest
{
    [JsonPropertyName("anno")]
    public int? Anno { get; set; }
    [JsonPropertyName("mese")]
    public int? Mese { get; set; }
    [JsonPropertyName("prodotto")]
    public string? Prodotto { get; set; }
    [JsonPropertyName("cap")]
    public string? Cap { get; set; }
    [JsonPropertyName("profilo")]
    public string? Profilo { get; set; }
    [JsonPropertyName("tipoNotifica")]
    public TipoNotifica? TipoNotifica { get; set; }
    [JsonPropertyName("statoContestazione")]
    public int[]? StatoContestazione { get; set; }
    [JsonPropertyName("iun")]
    public string? Iun { get; set; }
    [JsonPropertyName("recipientId")]
    public string? RecipientId { get; set; }
    [JsonPropertyName("idEnte")]
    public string? IdEnte { get; set; }
    [JsonPropertyName("ragioneSociale")]
    public string? RagioneSociale { get; set; }
    [JsonPropertyName("idContratto")]
    public string? IdContratto { get; set; }
    [JsonPropertyName("idReport")]
    public int? IdReport { get; set; }
    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }
}