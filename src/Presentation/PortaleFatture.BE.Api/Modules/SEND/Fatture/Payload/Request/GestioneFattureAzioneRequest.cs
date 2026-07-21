using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

 public class GestioneFattureAzioneRequest
 {
    public string? IdEnte { get; set; }
    public string? Azione { get; set; }
    public string? Anno { get; set; }
    public string? Mese { get; set; }
    public string? TipologiaFattura { get; set; }

    //public string IdUtente { get; set; }

    public List<NoteCommand>? Note { get; set; }
    public string? IdFattura { get; set; }




}



