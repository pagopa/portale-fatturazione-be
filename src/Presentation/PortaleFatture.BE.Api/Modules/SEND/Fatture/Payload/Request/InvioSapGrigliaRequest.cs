namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

/// <summary>
/// Filtri della griglia Invio a SAP (<c>POST api/fatture/invio-sap</c>) e dei due download
/// (<c>/download</c>, <c>/download/reinviate</c>). PF-735 RF-13, RF-15.
/// Tutto nullable: la validazione (anno obbligatorio, mesi 1-12, stato ammesso) sta in
/// <c>InvioSapRegole.ValidaFiltriGriglia</c>, non nel binding, cosi' un valore mancante da' 400 e non 500.
/// </summary>
public class InvioSapGrigliaRequest
{
    public int? Anno { get; set; }
    public List<int>? Mesi { get; set; }
    public List<string>? TipologieFattura { get; set; }
    /// <summary><c>TUTTE | DA_INVIARE | IN_ELABORAZIONE | INVIATE</c>; assente = <c>TUTTE</c>.</summary>
    public string? StatoInvio { get; set; }
    /// <summary>RF-13: solo fatture inviate piu' di una volta. Nel download re-inviate e' forzato a vero.</summary>
    public bool? SoloReinviate { get; set; }
}
