using PortaleFatture.BE.Core.Entities.Fatture;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;

public sealed class FatturaDistinctDto
{
    public string? TipologiaFattura { get; set; }
    public int Mese { get; set; }
    public int Anno { get; set; }
    public Dictionary<string, List<WorkFlowRequisitoFatture>?>? WorkFlow { get; set; }
} 