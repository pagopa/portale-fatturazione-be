using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Response;

public class ModuloCommessaPrevisionaleObbligatoriResponse
{
    public int MacrocategoriaVendita { get; set; }
    public string? DescrizioneMacrocategoriaVendita { get; set; }
    public List<ModuloCommessaPrevisionaleTotaleDto>? Lista { get; set; }
    public List<DatiModuloCommessaTotale>? Totali { get; set; }
}