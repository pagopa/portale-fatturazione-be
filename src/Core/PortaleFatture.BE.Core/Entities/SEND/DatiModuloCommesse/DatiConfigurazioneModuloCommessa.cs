namespace PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;

public class DatiConfigurazioneModuloCommessa
{
    public IEnumerable<DatiConfigurazioneModuloCategoriaCommessa>? Categorie { get; set; }
    public IEnumerable<DatiConfigurazioneModuloTipoCommessa>? Tipi { get; set; }
}