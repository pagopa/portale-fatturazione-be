namespace PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Payload.Response;

public record DatiFatturazioneResponse
{
    public long Id { get; set; }
    public string? Cup { get; set; }
    public bool NotaLegale { get; set; }
    public string? CodCommessa { get; set; }
    public DateTime? DataDocumento { get; set; }
    public bool? SplitPayment { get; set; }
    public string? IdEnte { get; set; }
    public string? IdDocumento { get; set; }
    public string? Map { get; set; }
    public string? TipoCommessa { get; set; }
    public string? Prodotto { get; set; }
    public string? Pec { get; set; }
    public DateTime DataCreazione { get; set; }
    public DateTime? DataModifica { get; set; }
    public IEnumerable<DatiFatturazioneContattoResponse>? Contatti { get; set; } 
    public string? CodiceSDI{ get; set; }
    public string? ContractCodiceSDI { get; set; }
}

public record DatiContractCodiceSDIResponse
{ 
    public string? ContractCodiceSDI { get; set; }
}