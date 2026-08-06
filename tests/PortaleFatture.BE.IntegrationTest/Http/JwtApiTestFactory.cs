using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Common;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Variante di ApiTestFactory che tiene lo schema JwtBearer REALE invece di TestAuthHandler.
///
/// ApiTestFactory sostituisce l'autenticazione con uno schema fittizio (ruolo via header) perche' il
/// suo scopo e' testare routing, [Authorize] e binding senza token veri. Cosi' pero' la pipeline
/// JwtBearer configurata da JwtAuthenticationConfiguration non viene mai eseguita: e' l'unico pezzo
/// del prodotto in cui la validazione del token e' delegata al middleware ASP.NET, ed e' proprio il
/// pezzo che una variazione dello stack Microsoft.IdentityModel.* potrebbe rompere.
/// Questa factory chiude quel buco: rimette JwtBearer come schema di default e conia i token.
///
/// I token sono firmati con la configurazione JWT REALE dell'app, letta dal suo container DI
/// (<see cref="ConfigurazioneJwt"/>), non con valori iniettati dal test: con l'hosting minimale una
/// sorgente di configurazione aggiunta da ConfigureAppConfiguration viene applicata dopo che Program
/// ha gia' legato PortaleFattureOptions, quindi non vincerebbe (e' lo stesso motivo per cui la
/// classe base ri-registra esplicitamente le factory del DB invece di limitarsi alla connection
/// string in configurazione). Il rovescio della medaglia e' che la sezione JWT deve essere
/// configurata sulla macchina che esegue i test (user secrets del progetto API): se manca o e'
/// inutilizzabile i test si auto-ignorano — v. <see cref="SkipSeConfigurazioneJwtAssente"/>.
/// </summary>
public class JwtApiTestFactory : ApiTestFactory
{
    /// <summary>Rotta protetta usata dai test: [Authorize(Roles=OPERATOR,ADMIN)] e in whitelist del nonce.</summary>
    public const string RottaProtetta = "api/auth/profilo";

    // HmacSha256 pretende una chiave di almeno 256 bit: sotto i 32 byte la firma non e' emettibile.
    private const int LunghezzaMinimaSecret = 32;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // La base ha messo TestAuthHandler come schema di default: qui si torna a JwtBearer, che
        // l'app ha gia' registrato in AddJwtOrApiKeyAuthentication con la configurazione reale.
        //
        // Serve PostConfigure, non un secondo AddAuthentication: ConfigureTestServices registra un
        // IStartupConfigureServicesFilter e i filtri vengono applicati in ordine INVERSO di
        // registrazione, quindi il callback della classe base (registrato prima) verrebbe eseguito
        // dopo il nostro e rimetterebbe TestAuthHandler come default. PostConfigure invece gira
        // sempre dopo tutti i Configure, qualunque sia l'ordine.
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            });
        });
    }

    /// <summary>Configurazione JWT con cui l'app valida davvero i token (null se non configurata).</summary>
    public JwtConfiguration? ConfigurazioneJwt =>
        Services.GetRequiredService<IPortaleFattureOptions>().JWT;

    /// <summary>
    /// Da chiamare in [SetUp]: senza una sezione JWT utilizzabile i token non sono emettibili e i
    /// test diventerebbero rossi per un problema di ambiente, non di prodotto (stessa logica di
    /// TestDb.SkipIfUnavailable per il container).
    /// </summary>
    public void SkipSeConfigurazioneJwtAssente()
    {
        var jwt = ConfigurazioneJwt;

        if (jwt?.ValidIssuer is null || jwt.ValidAudience is null || jwt.Secret is null)
            Assert.Ignore("Sezione PortaleFattureOptions:JWT non configurata (user secrets del progetto API).");

        if (jwt!.Secret!.Length < LunghezzaMinimaSecret)
            Assert.Ignore($"Secret JWT piu' corto di {LunghezzaMinimaSecret} caratteri: HmacSha256 non firmerebbe.");
    }

    public HttpClient CreateClientWithToken(string? token)
    {
        var client = CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    /// <summary>
    /// Token nella stessa forma di quelli emessi dall'API. I parametri permettono di deviare un solo
    /// aspetto alla volta (firma, issuer, audience, scadenza, ruolo) per isolare il motivo del rifiuto;
    /// il default e' un token valido per la configurazione reale dell'app.
    /// </summary>
    public string Token(
        string ruolo = Ruolo.ADMIN,
        string? issuer = null,
        string? audience = null,
        string? secret = null,
        DateTime? scadenza = null,
        string auth = AuthType.SELFCARE,
        string? profilo = null) // Profilo espone campi static, non const: non usabili come default
    {
        var jwt = ConfigurazioneJwt!;
        profilo ??= Profilo.PubblicaAmministrazione;

        var credenziali = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret ?? jwt.Secret!)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer ?? jwt.ValidIssuer,
            audience ?? jwt.ValidAudience,
            [
                new Claim(ClaimTypes.Name, "integration-test-user"),
                new Claim(ClaimTypes.Role, ruolo),
                new Claim(ClaimTypes.Email, "utente@test.it"),
                new Claim(CustomClaim.DescrizioneRuolo, ruolo),
                new Claim(CustomClaim.IdEnte, "11111111-1111-1111-1111-111111111111"),
                new Claim(CustomClaim.Prodotto, "prod-pn"),
                new Claim(CustomClaim.Profilo, profilo),
                new Claim(CustomClaim.GruppoRuolo, "gruppo-test"),
                new Claim(CustomClaim.NomeEnte, "Ente Test"),
                new Claim(CustomClaim.IdTipoContratto, "1"),
                new Claim(CustomClaim.Auth, auth)
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            expires: scadenza ?? DateTime.UtcNow.AddHours(1),
            signingCredentials: credenziali);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Secret valido come chiave ma diverso da quello dell'app, per il caso "firma non nostra".</summary>
    public static string SecretDiverso(string secretApp) =>
        new('x', Math.Max(secretApp.Length, LunghezzaMinimaSecret));
}
