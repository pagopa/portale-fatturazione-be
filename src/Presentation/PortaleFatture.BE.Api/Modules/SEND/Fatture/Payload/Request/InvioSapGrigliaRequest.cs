namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

/// <summary>
/// Filtri della griglia Invio a SAP (<c>POST api/fatture/invio-sap</c>) e di "Download Risultati"
/// (<c>/download</c>). Tutto nullable: la validazione sta in <c>InvioSapRegole.ValidaFiltriGriglia</c>, cosi' un
/// valore mancante da' 400 e non 500. <c>soloReinviate</c> e' stato tolto (v2.0): le re-inviate hanno il loro download.
/// </summary>
public class InvioSapGrigliaRequest
{
    public int? Anno { get; set; }
    public List<int>? Mesi { get; set; }
    public List<string>? TipologieFattura { get; set; }
    /// <summary><c>TUTTE | DA_INVIARE | INVIATE | IN_ELABORAZIONE</c>; assente = <c>TUTTE</c>.</summary>
    public string? StatoInvio { get; set; }
}
