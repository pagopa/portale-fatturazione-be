using System.Security;
using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Le due query di LETTURA dell'area ApiKeys — le uniche due cose che AuthMiddleware della
/// Integration API interroga per decidere se una richiesta entra:
///
///   ApiKeyQueryGetByKeyEnte  -> chiave valida? di quale ente?   (null => 401)
///   ApiKeyIpsQueryGet        -> IP autorizzati di quell'ente     (vuoto => 403)
///
/// Gli endpoint della function sono AuthorizationLevel.Anonymous: non c'è nessun controllo nativo
/// di Azure Functions dietro. Se queste due query sbagliano, sbaglia l'autenticazione — per questo
/// stanno su DB e non su mock: le regole vivono nell'SQL (gli INNER JOIN), non nel C#.
///
/// Il backlog le dava ancora scoperte: i command di SCRITTURA sono coperti da
/// ApiKeysCommandIntegrationTests, la validazione IP da ApiKeysVerifyIpTests (unit).
///
/// Sandbox: enti già nel seed, chiavi con prefisso riconoscibile, cleanup per ente (i valori a DB
/// sono cifrati, quindi una WHERE sul testo in chiaro non cancellerebbe nulla).
/// </summary>
public class ApiKeysQueryIntegrationTests
{
    private const string EnteAbilitato = "11111111-1111-1111-1111-111111111111";
    private const string EnteNonAbilitato = "22222222-2222-2222-2222-222222222222";
    private const string EnteTerzo = "33333333-3333-3333-3333-333333333333";
    private const string PrefissoChiave = "ITK-";
    private const string PrefissoIp = "203.0.113.";
    private const string ContrattoDiProva = "TOKEN-DUE-CONTRATTI";

    private IMediator _handler;
    private IAesEncryption _encryption;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
        _encryption = ServiceProvider.GetRequiredService<IAesEncryption>();
        Pulisci();
    }

    [TearDown]
    public void TearDown() => Pulisci();

    // ---------------------------------------------------------------------------------------------
    // ApiKeyQueryGetByKeyEnte — "questa chiave è valida, e di chi è?"
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task ChiavePerEnte_ChiaveValidaDiEnteAbilitato_ShouldRestituireIlContestoDellEnte()
    {
        InserisciChiave(EnteAbilitato, $"{PrefissoChiave}VALIDA");

        var dto = await _handler.Send(new ApiKeyQueryGetByKeyEnte { ApiKey = $"{PrefissoChiave}VALIDA" });

        Assert.That(dto, Is.Not.Null, "Chiave valida di ente abilitato: deve autenticare.");
        Assert.Multiple(() =>
        {
            Assert.That(dto!.IdEnte, Is.EqualTo(EnteAbilitato));
            // Sono i valori che il middleware mette in FunctionContext.Items e che finiscono nella
            // Session di ogni handler: se restano null, l'attività a valle lavora senza contesto.
            Assert.That(dto.RagioneSociale, Is.Not.Null.And.Not.Empty);
            Assert.That(dto.IdContratto, Is.Not.Null.And.Not.Empty);
        });
        // NB: dto.Prodotto è null su questo seed (pfd.Contratti.product non valorizzato per gli enti
        // di prova). Non è un difetto del prodotto e non viene asserito qui: se un domani servisse
        // coprirlo, va prima valorizzato nel seed.
    }

    /// <summary>
    /// 🔴 Una chiave sconosciuta NON restituisce null: fa lanciare
    /// `InvalidOperationException: Sequence contains no elements`, perché la persistence legge con
    /// SingleAsync (QuerySingleAsync di Dapper), che pretende esattamente una riga.
    ///
    /// Funziona lo stesso solo perché AuthMiddleware avvolge la chiamata in un try/catch che
    /// restituisce null, e null diventa 401. Due conseguenze che vale la pena conoscere:
    ///
    ///  - ogni tentativo con una chiave sbagliata — cioè ogni scansione, ogni typo, ogni chiave
    ///    ruotata e non aggiornata dal client — viene loggato con `_logger.LogError`. In
    ///    Application Insights un rifiuto ordinario è quindi indistinguibile da un guasto vero, ed è
    ///    il contrario della convenzione di progetto (le anomalie attese si loggano come
    ///    information — v. docs/architettura.md);
    ///  - qualunque altro consumatore di questa query che NON abbia quel try/catch riceverebbe
    ///    un'eccezione dove si aspetta un null.
    ///
    /// Il rimedio naturale è SingleOrDefaultAsync, che DapperBase espone già (aggiunto con
    /// l'incidente di vwRelDettaglio, v. docs/viste-endpoint.md): qui "non trovato" è un esito
    /// legittimo, non un'anomalia dei dati.
    /// </summary>
    [Test]
    public void ChiavePerEnte_ChiaveInesistente_Lancia_Caratterizzazione()
    {
        var eccezione = Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Send(new ApiKeyQueryGetByKeyEnte { ApiKey = $"{PrefissoChiave}MAI-VISTA" }));

        Assert.That(eccezione!.Message, Does.Contain("no elements"),
            "Se questo è diventato un null, la query è passata a SingleOrDefaultAsync: riscrivere l'asserzione.");
    }

    /// <summary>
    /// Il gate di abilitazione non è nel C#: è l'INNER JOIN su pfw.EntiApiKeys con Attiva = 1. Un
    /// ente con una chiave perfettamente valida ma non abilitato alle Integration API non entra —
    /// e ci si arriva anche solo mettendo Attiva a 0, senza toccare le chiavi.
    ///
    /// È una regola che sparirebbe in silenzio se qualcuno "semplificasse" quella query.
    /// </summary>
    [Test]
    public void ChiavePerEnte_EnteNonAbilitato_NonAutentica_AncheConChiaveValida()
    {
        InserisciChiave(EnteNonAbilitato, $"{PrefissoChiave}ENTE-NON-ABILITATO");

        // Stesso meccanismo del test precedente: l'INNER JOIN non produce righe e SingleAsync lancia.
        // Ciò che conta qui è che l'ente NON entri: la chiave esiste in pfw.ApiKeys, ma l'ente non è
        // in EntiApiKeys con Attiva = 1.
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Send(new ApiKeyQueryGetByKeyEnte { ApiKey = $"{PrefissoChiave}ENTE-NON-ABILITATO" }));
    }

    [TestCase("")]
    [TestCase(null)]
    public async Task ChiavePerEnte_ChiaveAssente_ShouldRestituireNull_SenzaInterrogareIlDb(string? chiave)
    {
        var dto = await _handler.Send(new ApiKeyQueryGetByKeyEnte { ApiKey = chiave });

        Assert.That(dto, Is.Null);
    }

    /// <summary>
    /// 🔴 ADVERSARIAL — un ente con PIÙ DI UN CONTRATTO non riesce ad autenticarsi.
    ///
    /// SelectKeyByEnte fa `inner join pfd.contratti c on e.InternalIstitutionId = c.InternalIstitutionId`
    /// SENZA vincolo su prodotto o tipo contratto, e la persistence legge con SingleAsync. Due
    /// contratti = due righe = eccezione.
    ///
    /// Nel middleware quell'eccezione finisce nel try/catch di IsValidApiKey, che restituisce null:
    /// il chiamante riceve quindi "Unauthorized: Invalid or missing API Key", cioè la stessa risposta
    /// di una chiave sbagliata. Chi assiste l'aderente cercherà una chiave revocata o un IP fuori
    /// whitelist, e la causa è invece l'anagrafica.
    ///
    /// Non è teorico: è la stessa forma del fan-out già visto su vwRelDettaglio (LEFT JOIN solo su
    /// FkIdEnte) e documentato in docs/viste-endpoint.md. Qui però il sintomo non è un 500 ma un 401,
    /// che è molto più difficile da ricondurre alla causa.
    ///
    /// Caratterizzazione: se un domani la query filtrerà il contratto, questo test diventa rosso ed è
    /// il segnale che il difetto è stato chiuso.
    /// </summary>
    [Test]
    public void ChiavePerEnte_EnteConDueContratti_NonAutentica_Caratterizzazione()
    {
        InserisciChiave(EnteAbilitato, $"{PrefissoChiave}DUE-CONTRATTI");
        AggiungiSecondoContratto(EnteAbilitato);

        var eccezione = Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Send(new ApiKeyQueryGetByKeyEnte { ApiKey = $"{PrefissoChiave}DUE-CONTRATTI" }));

        Assert.That(eccezione!.Message, Does.Contain("more than one element"),
            "È il fan-out del join su pfd.Contratti. Nel middleware diventa un 401 indistinguibile "
            + "da una chiave sbagliata. Se questo test è diventato verde-diverso, la query è stata "
            + "corretta: riscrivere l'asserzione sul DTO restituito.");
    }

    /// <summary>
    /// ⚠️ La persistence CIFRA IN PLACE la proprietà del command (`_command.ApiKey = Encrypt(...)`)
    /// invece di usare una variabile locale. Inviare due volte lo stesso oggetto command non
    /// funziona: la seconda volta si cifra un valore già cifrato e non corrisponde più a nulla.
    ///
    /// Oggi non si manifesta perché MediatR riceve un command nuovo a ogni richiesta, ma è una mina
    /// per chiunque provi a riusare l'oggetto — per esempio scrivendo un retry.
    /// </summary>
    [Test]
    public async Task ChiavePerEnte_StessoCommandInviatoDueVolte_LaSecondaNonTrovaNulla_Caratterizzazione()
    {
        InserisciChiave(EnteAbilitato, $"{PrefissoChiave}RIUSO");
        var command = new ApiKeyQueryGetByKeyEnte { ApiKey = $"{PrefissoChiave}RIUSO" };

        var primo = await _handler.Send(command);
        Assert.That(primo, Is.Not.Null, "Il primo invio funziona.");

        // Il secondo non trova nulla — e "nulla", come sopra, significa eccezione.
        Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Send(command),
            "La chiave è stata cifrata due volte: il valore cercato non esiste a DB.");
    }

    // ---------------------------------------------------------------------------------------------
    // ApiKeyIpsQueryGet — "quali IP sono autorizzati per questo ente?"
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task IpDellEnte_ShouldRestituirliInChiaro_OrdinatiPerDataCreazione()
    {
        InserisciIp(EnteAbilitato, $"{PrefissoIp}10", DateTime.Now.AddMinutes(-10));
        InserisciIp(EnteAbilitato, $"{PrefissoIp}11", DateTime.Now.AddMinutes(-5));

        var ips = await _handler.Send(new ApiKeyIpsQueryGet(Auth(EnteAbilitato)));

        Assert.That(ips, Is.Not.Null);
        // A DB sono cifrati: se tornassero cifrati, il confronto del middleware con l'IP del
        // chiamante non combacerebbe MAI e nessuno entrerebbe più.
        Assert.That(ips!.Select(x => x.IpAddress).ToList(),
            Is.EqualTo(new[] { $"{PrefissoIp}10", $"{PrefissoIp}11" }).AsCollection);
    }

    /// <summary>
    /// Nessun IP in whitelist è il caso che nel middleware diventa 403: va distinto da "ente
    /// inesistente", che è comunque una lista vuota. Il middleware non li distingue, ed è corretto
    /// che non lo faccia — ma qui si verifica che la query non lanci.
    /// </summary>
    [Test]
    public async Task IpDellEnte_SenzaAlcunIp_ShouldRestituireListaVuota()
    {
        var ips = await _handler.Send(new ApiKeyIpsQueryGet(Auth(EnteTerzo)));

        Assert.That(ips, Is.Not.Null.And.Empty, "Nel middleware questo diventa 403.");
    }

    /// <summary>
    /// Isolamento fra aderenti: è la proprietà per cui esiste tutta questa area. Un IP registrato da
    /// un ente non deve comparire fra quelli di un altro.
    /// </summary>
    [Test]
    public async Task IpDellEnte_ShouldEssereIsolatiPerEnte()
    {
        InserisciIp(EnteAbilitato, $"{PrefissoIp}20", DateTime.Now);

        var altrui = await _handler.Send(new ApiKeyIpsQueryGet(Auth(EnteTerzo)));

        Assert.That(altrui, Is.Not.Null.And.Empty);
    }

    /// <summary>
    /// La query degli IP ha una guardia che quella delle chiavi non ha: l'handler verifica prima che
    /// l'ente sia abilitato e solleva SecurityException("Ente non registrato!") — che il gestore
    /// globale dell'API mappa su 401.
    ///
    /// Le due query dell'area segnalano quindi il rifiuto in due modi diversi: una lancia
    /// InvalidOperationException dal layer Dapper, l'altra SecurityException dall'handler. Nel
    /// middleware della function la differenza si perde (la prima è dentro un try/catch, la seconda
    /// no), ma per chi legge i log sono due cose distinte.
    /// </summary>
    [Test]
    public void IpDellEnte_SenzaIdEnte_ShouldSollevareSecurityException()
    {
        Assert.ThrowsAsync<SecurityException>(() => _handler.Send(new ApiKeyIpsQueryGet(Auth(string.Empty))));
    }

    // ---------------------------------------------------------------------------------------------

    private static AuthenticationInfo Auth(string idEnte) => new()
    {
        Id = "integration-test-apikeys-query",
        IdEnte = idEnte,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    /// <summary>
    /// Inserimento diretto invece che tramite CreateORModifyApiKeyCommand: qui si testano le
    /// LETTURE, e passare dal command legherebbe questi test alle sue regole (massimo due chiavi per
    /// ente, ramo di rotazione) che non c'entrano nulla con ciò che si vuole verificare.
    /// La cifratura è la stessa del prodotto, presa dal container DI.
    /// </summary>
    private void InserisciChiave(string idEnte, string chiaveInChiaro)
    {
        Esegui(
            "INSERT INTO pfw.ApiKeys (FkIdEnte, ApiKey, DataCreazione, Attiva) VALUES (@ente, @chiave, GETDATE(), 1)",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@ente", idEnte);
                cmd.Parameters.AddWithValue("@chiave", _encryption.EncryptString(chiaveInChiaro));
            });
    }

    private void InserisciIp(string idEnte, string ipInChiaro, DateTime creazione)
    {
        Esegui(
            "INSERT INTO pfw.ApiKeysIPs (FkIdEnte, IPAddress, DataCreazione) VALUES (@ente, @ip, @data)",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@ente", idEnte);
                cmd.Parameters.AddWithValue("@ip", _encryption.EncryptString(ipInChiaro));
                cmd.Parameters.AddWithValue("@data", creazione);
            });
    }

    /// <summary>
    /// Copia il contratto esistente dell'ente cambiando solo la chiave, per ottenere il fan-out del
    /// join. Si crea qui e non nel seed condiviso perché un secondo contratto su un ente del seed
    /// cambierebbe l'esito dei test di altre aree.
    /// </summary>
    private static void AggiungiSecondoContratto(string idEnte)
    {
        Esegui(@"
INSERT INTO pfd.Contratti (InternalIstitutionId, onboardingtokenid, FkIdTipoContratto, product, codiceSDI)
SELECT TOP 1 InternalIstitutionId, @token, FkIdTipoContratto, product, codiceSDI
  FROM pfd.Contratti WHERE InternalIstitutionId = @ente;",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@ente", idEnte);
                cmd.Parameters.AddWithValue("@token", ContrattoDiProva);
            });
    }

    private static void Esegui(string sql, Action<SqlCommand> parametri)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        parametri(cmd);
        cmd.ExecuteNonQuery();
    }

    private static void Pulisci()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        // Per ENTE: i valori a DB sono cifrati, una WHERE sul testo in chiaro non cancellerebbe nulla.
        cmd.CommandText = @"
DELETE FROM pfw.ApiKeysIPs WHERE FkIdEnte IN (@e1, @e2, @e3);
DELETE FROM pfw.ApiKeys    WHERE FkIdEnte IN (@e1, @e2, @e3);
DELETE FROM pfd.Contratti  WHERE onboardingtokenid = @token;";
        cmd.Parameters.AddWithValue("@e1", EnteAbilitato);
        cmd.Parameters.AddWithValue("@e2", EnteNonAbilitato);
        cmd.Parameters.AddWithValue("@e3", EnteTerzo);
        cmd.Parameters.AddWithValue("@token", ContrattoDiProva);
        cmd.ExecuteNonQuery();
    }
}
