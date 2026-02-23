using System.Text.Json.Serialization;
using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.Contestazioni.Payload;

public sealed class CheckDownloadNotificheInternalRequest
{ 
    public DateTime? DataVerifica { get; set; }
    public Session? Session { get; set; } 
    public int Anno { get; set; }
    public int Mese { get; set; }
} 