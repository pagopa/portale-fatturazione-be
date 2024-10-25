namespace PortaleFatture.BE.Api.Modules.SEND.DatiConfigurazioneModuloCommesse.Response;

public record DatiConfigurazioneModuloCommessaResponse
{
    public IEnumerable<DatiConfigurazioneModuloCategoriaCommessaResponse>? Categorie { get; set; }
    public IEnumerable<DatiConfigurazioneModuloTipoCommessaResponse>? Tipi { get; set; }
}

public record DatiConfigurazioneModuloCategoriaCommessaResponse
{
    public int IdCategoriaSpedizione { get; set; }
    public int Percentuale { get; set; }
    public string? Descrizione { get; set; }
}

public record DatiConfigurazioneModuloTipoCommessaResponse
{
    public int IdTipoSpedizione { get; set; }
    public decimal MediaNotificaNazionale { get; set; }
    public decimal MediaNotificaInternazionale { get; set; }
    public string? Descrizione { get; set; }
}