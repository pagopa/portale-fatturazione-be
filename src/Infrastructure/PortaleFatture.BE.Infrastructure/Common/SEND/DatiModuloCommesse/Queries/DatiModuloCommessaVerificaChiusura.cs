using MediatR;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;

public class DatiModuloCommessaVerificaChiusura : IRequest<bool>
{
    public int Anno { get; set; }
    public int Mese { get; set; }
}
