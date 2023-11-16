using MediatR;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

public class DatiConfigurazioneModuloCommessaCreateCommand : IRequest<DatiConfigurazioneModuloCommessa?>
{
    public List<DatiConfigurazioneModuloCommessaCreateTipoCommand>? Tipi { get; set; }
    public List<DatiConfigurazioneModuloCommessaCreateCategoriaCommand>? Categorie { get; set; }
}

public class DatiConfigurazioneModuloCommessaCreateTipoCommand
{
    public long IdTipoContratto { get; set; }
    public string? Prodotto { get; set; }
    public int TipoSpedizione { get; set; }
    public decimal MediaNotificaNazionale { get; set; }
    public decimal MediaNotificaInternazionale { get; set; }
    public DateTime? DataCreazione { get; set; }
    public DateTime DataInizioValidita { get; set; }
    public string? Descrizione { get; set; }
}

public class DatiConfigurazioneModuloCommessaCreateCategoriaCommand
{
    public long IdTipoContratto { get; set; }
    public string? Prodotto { get; set; }
    public int IdCategoriaSpedizione { get; set; }
    public int Percentuale { get; set; } 
    public DateTime? DataCreazione { get; set; }
    public DateTime DataInizioValidita { get; set; }
    public string? Descrizione { get; set; }
}