using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure;
using PortaleFatture.BE.Infrastructure.Common.Identity;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Test ADVERSARIAL sul DECODER del JWT, non sulle regole di validazione.
///
/// Perché sono distinti da JwtBearerConfigurationTests: quelli usano token **ben formati** con firma,
/// issuer, audience o scadenza sbagliati — arrivano al decoder senza stress e verificano le regole.
/// Qui il token è **malformato a livello di codifica**: segmenti non base64url, token troncato,
/// segmenti vuoti, payload spropositato.
///
/// È il livello dove gira `Microsoft.Bcl.Memory`, l'unica delle librerie pinnate il cui codice viene
/// davvero eseguito: è il backport di `System.Buffers.Text.Base64Url` che IdentityModel usa per
/// spacchettare i tre segmenti (su net8 quel tipo non esiste nel runtime, entra dalla 9). Un difetto
/// del backport si manifesterebbe qui e non nei test sulle regole.
///
/// L'invariante non è "quale eccezione": è che ogni input ostile venga **rifiutato in modo
/// controllato** — un'eccezione della famiglia SecurityToken/Argument, che il middleware traduce in
/// 401 — e **mai** con un errore inatteso, un valore accettato o un blocco. Per questo si usa
/// `Assert.Catch` su tipi larghi, come già fa JwtBearerConfigurationTests: i test devono restare
/// validi anche alzando o abbassando lo stack IdentityModel (v. TD-3).
/// </summary>
public class JwtDecoderAdversarialTests
{
    private const string Secret = "chiave-di-test-lunga-almeno-32-caratteri!";
    private const string Issuer = "portale-fatturazione-test";
    private const string Audience = "portale-fatturazione-client";

    /// <summary>Oltre questo tempo si considera un blocco, non una validazione lenta.</summary>
    private static readonly TimeSpan LimiteDiTempo = TimeSpan.FromSeconds(5);

    private static JwtConfiguration Configurazione() =>
        new() { ValidIssuer = Issuer, ValidAudience = Audience, Secret = Secret };

    private static TokenValidationParameters Parametri() =>
        new JwtBearerOptions().JwtAuthenticationConfiguration(Configurazione()).TokenValidationParameters;

    private static void Valida(string token) =>
        new JwtSecurityTokenHandler().ValidateToken(token, Parametri(), out _);

    /// <summary>
    /// Token valido, da cui derivare le mutazioni.
    ///
    /// ⚠️ Il set di claim dev'essere COMPLETO: `JwtTokenService.GenerateJwtToken` passa da
    /// `IdentityExtensions.Mapper`, che accede ai claim senza controlli e va in NullReferenceException
    /// se ne manca uno. È la stessa trappola già annotata per `Http/JwtApiTestFactory` in
    /// `docs/test-integrazione-db-seedato.md`: sembra un errore del test, è un mapper senza difese.
    /// </summary>
    private static string TokenValido() =>
        new JwtTokenService(Configurazione()).GenerateJwtToken(
        [
            new Claim(ClaimTypes.Name, "adversarial"),
            new Claim(ClaimTypes.Role, Ruolo.ADMIN),
            new Claim(ClaimTypes.Email, "adversarial@test.it"),
            new Claim(CustomClaim.DescrizioneRuolo, "Amministratore"),
            new Claim(CustomClaim.IdEnte, "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            new Claim(CustomClaim.Prodotto, "prod-pn"),
            new Claim(CustomClaim.Profilo, "PA"),
            new Claim(CustomClaim.GruppoRuolo, "gruppo-test"),
            new Claim(CustomClaim.Auth, "SELFCARE"),
        ]).JWT!;

    // ---------------------------------------------------------------------------------------------
    // Forma del token
    // ---------------------------------------------------------------------------------------------

    [TestCase("", TestName = "decoder · stringa vuota")]
    [TestCase("   ", TestName = "decoder · soli spazi")]
    [TestCase("non-un-token", TestName = "decoder · nessun punto")]
    [TestCase("a.b", TestName = "decoder · due soli segmenti")]
    [TestCase("a.b.c.d", TestName = "decoder · quattro segmenti")]
    [TestCase("..", TestName = "decoder · tre segmenti vuoti")]
    [TestCase(".", TestName = "decoder · un solo punto")]
    public void FormaNonValida_ShouldEssereRifiutataInModoControllato(string token)
        => AssertRifiutoControllato(token);

    [Test]
    public void SegmentiNonBase64Url_ShouldEssereRifiutati()
    {
        // '+' e '/' appartengono al base64 STANDARD, non a quello url-safe usato dai JWT: sono
        // esattamente i caratteri su cui un decoder url-safe scritto male può sbagliare.
        AssertRifiutoControllato("eyJhbGciOiJIUzI1NiJ9.++++////.firma");
    }

    [Test]
    public void PayloadConPaddingEsplicito_ShouldEssereRifiutato()
    {
        // Il base64url dei JWT non ammette il padding '=': un decoder tollerante potrebbe accettarlo
        // e produrre claim diversi da quelli firmati.
        var parti = TokenValido().Split('.');

        AssertRifiutoControllato($"{parti[0]}.{parti[1]}==.{parti[2]}");
    }

    [Test]
    public void TokenTroncato_ShouldEssereRifiutato()
    {
        var valido = TokenValido();

        AssertRifiutoControllato(valido[..(valido.Length / 2)]);
    }

    [Test]
    public void PayloadNonJson_ShouldEssereRifiutato()
    {
        // Segmento decodificabile come base64url ma il cui contenuto non è JSON: il decoder passa,
        // il parser no. Deve restare un rifiuto, non un'eccezione di tipo inatteso.
        var parti = TokenValido().Split('.');
        var spazzatura = Base64Url(Encoding.UTF8.GetBytes("questo non e' json"));

        AssertRifiutoControllato($"{parti[0]}.{spazzatura}.{parti[2]}");
    }

    // ---------------------------------------------------------------------------------------------
    // Dimensione
    // ---------------------------------------------------------------------------------------------

    [Test]
    public void PayloadSpropositato_ShouldEssereRifiutatoSenzaBloccarsi()
    {
        // ~1 MB di payload: il decoder non deve degenerare. Il limite di tempo è la parte che conta —
        // un rifiuto è accettabile, un blocco no.
        var parti = TokenValido().Split('.');
        var enorme = Base64Url(Encoding.UTF8.GetBytes("{\"x\":\"" + new string('a', 1_000_000) + "\"}"));

        AssertRifiutoControllato($"{parti[0]}.{enorme}.{parti[2]}");
    }

    [Test]
    public void TokenConMoltissimiSegmenti_ShouldEssereRifiutatoSenzaBloccarsi()
    {
        AssertRifiutoControllato(string.Join('.', Enumerable.Repeat("eyJhIjoiYiJ9", 10_000)));
    }

    // ---------------------------------------------------------------------------------------------
    // Manomissione dei claim (il decoder passa, la firma no)
    // ---------------------------------------------------------------------------------------------

    [Test]
    public void PayloadSostituitoConUnoValido_ShouldEssereRifiutatoPerFirma()
    {
        // Il caso classico: payload sostituito con JSON legittimo — ruolo ADMIN — tenendo la firma
        // originale. Il decoder lo spacchetta senza problemi: a fermarlo dev'essere la firma.
        var parti = TokenValido().Split('.');
        var elevato = Base64Url(Encoding.UTF8.GetBytes(
            $$"""{"{{ClaimTypes.Role}}":"{{Ruolo.ADMIN}}","iss":"{{Issuer}}","aud":"{{Audience}}","exp":253402300799}"""));

        var eccezione = AssertRifiutoControllato($"{parti[0]}.{elevato}.{parti[2]}");

        Assert.That(eccezione, Is.InstanceOf<SecurityTokenInvalidSignatureException>()
                                 .Or.InstanceOf<SecurityTokenSignatureKeyNotFoundException>()
                                 .Or.InstanceOf<SecurityTokenMalformedException>(),
            "Un payload riscritto deve cadere sulla firma: se passasse, chiunque potrebbe farsi ADMIN.");
    }

    [Test]
    public void AlgNone_ShouldEssereRifiutato()
    {
        // Attacco storico su JWT: header con alg "none" e firma vuota, per farsi accettare un token
        // non firmato. Deve essere respinto perché la configurazione pretende la firma.
        var header = Base64Url(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""));
        var payload = Base64Url(Encoding.UTF8.GetBytes(
            $$"""{"{{ClaimTypes.Role}}":"{{Ruolo.ADMIN}}","iss":"{{Issuer}}","aud":"{{Audience}}","exp":253402300799}"""));

        AssertRifiutoControllato($"{header}.{payload}.");
    }

    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifica il contratto comune: rifiuto con un'eccezione delle famiglie che il middleware sa
    /// tradurre in 401, entro un tempo ragionevole. Restituisce l'eccezione per i casi che vogliono
    /// asserire anche il motivo.
    /// </summary>
    private static Exception AssertRifiutoControllato(string token)
    {
        var cronometro = Stopwatch.StartNew();
        var eccezione = Assert.Catch(() => Valida(token));
        cronometro.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(eccezione, Is.Not.Null, "Il token ostile è stato ACCETTATO: è il caso peggiore.");

            Assert.That(eccezione, Is.InstanceOf<SecurityTokenException>()
                                     .Or.InstanceOf<ArgumentException>()
                                     .Or.InstanceOf<FormatException>(),
                $"Rifiuto non controllato: {eccezione?.GetType().Name}. Deve essere un'eccezione che il "
                + "middleware traduce in 401, non un errore inatteso che diventerebbe un 500.");

            Assert.That(cronometro.Elapsed, Is.LessThan(LimiteDiTempo),
                $"Validazione durata {cronometro.ElapsedMilliseconds} ms: un input ostile non deve poter "
                + "occupare un thread del server.");
        });

        TestContext.Out.WriteLine($"{eccezione!.GetType().Name} in {cronometro.ElapsedMilliseconds} ms");
        return eccezione;
    }

    private static string Base64Url(byte[] dati) =>
        Convert.ToBase64String(dati).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
