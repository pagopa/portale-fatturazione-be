using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.Contestazioni.Payload;

public sealed class ReportContestazioneStepsInternalRequest
{
    public Session? Session { get; set; }
    public int IdReport { get; set; }
} 