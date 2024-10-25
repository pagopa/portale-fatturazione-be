using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Commands;

public class DatiModuloCommessaTotaleCreateListCommand : IRequest<List<DatiModuloCommessaTotale>>
{
    public IAuthenticationInfo? AuthenticationInfo { get; set; }

    public List<DatiModuloCommessaTotaleCreateCommand>? DatiModuloCommessaTotaleListCommand { get; set; }
    public Dictionary<long, ParzialiTipoCommessa>? ParzialiTipoCommessa { get; set; }
}

public class ParzialiTipoCommessa()
{
    public decimal ValoreNazionali { get; set; }
    public decimal PrezzoNazionali { get; set; }
    public decimal ValoreInternazionali { get; set; }
    public decimal PrezzoInternazionali { get; set; }
}