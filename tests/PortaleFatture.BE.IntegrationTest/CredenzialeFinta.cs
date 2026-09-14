using Azure.Core;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Credenziale che restituisce un token fisso senza toccare Entra ID. Serve dove si costruisce il
/// <c>LanguageService</c> vero ma **non** si vuole dipendere dall'identita' di chi esegue: con
/// <c>DefaultAzureCredential</c> l'esito cambierebbe fra una macchina con <c>az login</c> e una senza, e
/// la catena di credenziali puo' impiegare decine di secondi prima di arrendersi.
///
/// Contro Azure reale il token finto viene rifiutato con un 401: e' il modo, a costo zero, di provare
/// il ramo "identita' rifiutata".
/// </summary>
internal sealed class CredenzialeFinta : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => new("token-finto-per-il-test", DateTimeOffset.UtcNow.AddHours(1));

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => new(GetToken(requestContext, cancellationToken));
}
