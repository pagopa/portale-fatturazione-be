namespace PortaleFatture.BE.Core.Exceptions;

/// <summary>
/// Un servizio esterno a monte non ha risposto entro il tempo che gli concediamo. Il gestore globale
/// la traduce in **504 Gateway Timeout**.
///
/// È una <see cref="UpstreamServiceException"/> più specifica, e la distinzione serve a chi legge i
/// log: un 502 dice "ha risposto male" (quota, credenziale, errore), un 504 dice "non ha risposto in
/// tempo" — cause diverse, rimedi diversi.
///
/// ⚠️ **Nel gestore globale va elencata PRIMA della classe base**, altrimenti il pattern matching di
/// `UpstreamServiceException` la cattura per prima e il 504 non viene mai emesso.
///
/// Perché esiste: davanti a questo backend c'è un gateway che tronca le chiamate sincrone intorno al
/// minuto (v. `docs/autenticazione.md`). Senza un timeout **nostro**, più basso del suo, un'operazione
/// lenta non produce un errore gestito ma una connessione tagliata a metà — il client vede un errore
/// generico del gateway, nei nostri log non resta nulla, e nel frattempo abbiamo tenuto occupato un
/// thread e pagato la chiamata al servizio esterno.
/// </summary>
public class UpstreamTimeoutException : UpstreamServiceException
{
    public UpstreamTimeoutException()
    {
    }

    public UpstreamTimeoutException(string message) : base(message)
    {
    }

    public UpstreamTimeoutException(string message, Exception ex) : base(message, ex)
    {
    }
}
