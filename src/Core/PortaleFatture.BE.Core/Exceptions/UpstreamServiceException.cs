namespace PortaleFatture.BE.Core.Exceptions;

/// <summary>
/// Un servizio esterno **a monte** ha fallito: quota esaurita, credenziale revocata, endpoint
/// irraggiungibile, risposta non valida. Il gestore globale la traduce in **502 Bad Gateway**.
///
/// Serve a distinguere due cose che altrimenti arrivano al client identiche:
/// <list type="bullet">
///   <item>l'elaborazione è andata a buon fine e **non ha prodotto risultati** → 404;</item>
///   <item>l'elaborazione **non è avvenuta** perché il servizio a monte ha fallito → 502.</item>
/// </list>
///
/// La differenza non è accademica: nel primo caso il chiamante può cambiare input e riprovare, nel
/// secondo no — e chi guarda i log deve sapere se sta osservando un dato assente o un guasto.
/// Da non confondere con il **503**, che questo backend usa per "il servizio non è configurato su
/// questo ambiente": lì manca la configurazione, qui la configurazione c'è e la chiamata è fallita.
/// </summary>
public class UpstreamServiceException : Exception
{
    public UpstreamServiceException()
    {
    }

    public UpstreamServiceException(string message) : base(message)
    {
    }

    public UpstreamServiceException(string message, Exception ex) : base(message, ex)
    {
    }
}
