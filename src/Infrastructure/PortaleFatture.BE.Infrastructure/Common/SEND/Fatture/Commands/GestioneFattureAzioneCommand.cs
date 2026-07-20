
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;
namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

 public class GestioneFattureAzioneCommand(IAuthenticationInfo authenticationInfo) : IRequest<bool?>
 {

    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public string IdEnte { get; set; }
    public string Azione { get; set; }
    public string Anno { get; set; }
    public string Mese { get; set; }
    public string TipologiaFattura { get; set; }

    public string IdUtente { get; set; }
    public List<NoteCommand> Note { get; set; }

    public string IdFattura { get; set; }
}

public class NoteCommand
{
    public Guid IdNota { get; set; }
    public DateTime Data { get; set; }
    public string Testo { get; set; }
}

