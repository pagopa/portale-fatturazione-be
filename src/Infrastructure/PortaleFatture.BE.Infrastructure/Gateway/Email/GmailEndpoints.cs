namespace PortaleFatture.BE.Infrastructure.Gateway.Email;

/// <summary>
/// Indirizzi dei servizi Google usati da <see cref="PspEmailSender"/> e <see cref="AlertSender"/>.
/// I valori predefiniti sono quelli reali di Google e sono gli unici usati dall'applicazione: nessuna
/// impostazione di ambiente li cambia, di proposito, perche' a questi indirizzi viaggiano il refresh
/// token e il client secret. Un valore diverso si passa solo dal costruttore, ed esiste per i test,
/// che mettono un server finto al posto di Google.
/// </summary>
public sealed record GmailEndpoints(
    string? ApiBaseUri = null,
    string TokenInfoUrl = "https://www.googleapis.com/oauth2/v1/tokeninfo",
    string TokenUrl = "https://oauth2.googleapis.com/token")
{
    public static readonly GmailEndpoints Google = new();
}
