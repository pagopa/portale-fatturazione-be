namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;
public class PipelineParameters()
{
    public int AnnoRiferimento { get; set; }
    public int MeseRiferimento { get; set; }
    public string? TipologiaFattura { get; set; }
}

public class RequestPipeline()
{
    public PipelineParameters? Parameters { get; set; }
} 