using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Commands;

public class DatiConfigurazioneModuloCommessaUpdateCommand : IRequest<DatiConfigurazioneModuloCommessa?>
{
    public DatiConfigurazioneModuloCommessaUpdateTipoCommand? Tipo { get; set; }
    public DatiConfigurazioneModuloCommessaUpdateCategoriaCommand? Categoria { get; set; }
}

public class DatiConfigurazioneModuloCommessaUpdateTipoCommand
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public long IdTipoContratto { get; set; }
    public string? Prodotto { get; set; }
    public DateTime DataInizioValidita { get; set; }
    public DateTime DataFineValidita { get; set; }
    public DateTime DataModifica { get; set; }
}

public class DatiConfigurazioneModuloCommessaUpdateCategoriaCommand
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public long IdTipoContratto { get; set; }
    public string? Prodotto { get; set; }
    public DateTime DataInizioValidita { get; set; }
    public DateTime DataFineValidita { get; set; }
    public DateTime DataModifica { get; set; }
}