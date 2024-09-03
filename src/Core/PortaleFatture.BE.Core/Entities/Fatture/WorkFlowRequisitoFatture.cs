using PortaleFatture.BE.Core.Entities.DatiRel;

namespace PortaleFatture.BE.Core.Entities.Fatture;

public class WorkFlowRequisitoFatture
{
    public string? TipologiaFattura { get; set; }
    public int Ordine { get; set; }
    public int? Condition { get; set; }
    public string? ExtraCondition { get; set; }
}