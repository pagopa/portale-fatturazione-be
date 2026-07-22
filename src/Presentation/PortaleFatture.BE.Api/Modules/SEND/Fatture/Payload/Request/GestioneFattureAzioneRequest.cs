using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

 public class GestioneFattureAzioneRequest
 {
    public string? IdEnte { get; set; }
    public string? Azione { get; set; }
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public string? TipologiaFattura { get; set; }

    //public string IdUtente { get; set; }

    public NoteCommand? Nota { get; set; }
    public string? IdFattura { get; set; }




}



