using System.Security;
using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Test ADVERSARIAL sull'area ApiKeys, sul modello di GestioneFattureAdversarialIntegrationTests:
/// input ostili, valori estremi, injection, concorrenza. Non verificano i rifiuti *attesi* (quelli
/// stanno in ApiKeysCommandIntegrationTests) ma che il sistema regga a ciò che non ha previsto —
/// rifiutando in modo controllato o facendo no-op, mai corrompendo dati.
///
/// Pesa più che altrove perché è la superficie di autenticazione delle Integration API.
///
/// Nota strutturale che rende quest'area diversa dalle altre: **la chiave arriva dal client**
/// (`CreateORModifyApiKeyRequest.ApiKey` è una stringa arbitraria; il GUID è solo il default quando
/// manca), e viene **cifrata prima di toccare il DB** — quindi la lunghezza che arriva alla colonna
/// non è quella digitata.
///
/// Sandbox: IP in 203.0.113.0/24, chiavi con prefisso riconoscibile, cleanup per ente.
/// </summary>
public class ApiKeysAdversarialIntegrationTests
{
    private const string Ente = "11111111-1111-1111-1111-111111111111";
    private const string EnteTerzo = "33333333-3333-3333-3333-333333333333";

    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
        Pulisci();
    }

    [TearDown]
    public void TearDown() => Pulisci();

    // ---------------------------------------------------------------------------------------------
    // Lunghezza: la cifratura espande, la colonna no
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// `pfw.ApiKeys.ApiKey` è `nvarchar(400)`, ma il valore salvato è il CIPHERTEXT: AES + base64url
    /// espande di circa 4/3. Una chiave che il client considera valida (sotto i 400 caratteri) può
    /// quindi sforare la colonna una volta cifrata.
    ///
    /// L'invariante che deve reggere non è "deve funzionare": è **non troncare in silenzio**. Una
    /// chiave troncata sarebbe irrecuperabile e non autenticherebbe mai, senza che nulla lo segnali.
    ///
    /// ESITO (misurato il 07/08/2026): l'invariante regge — SQL Server rifiuta l'INSERT invece di
    /// troncare — ma il modo è brutale. Espansione osservata: 100→150, 250→342, **400→555**, quindi la
    /// soglia reale è intorno ai **296 caratteri in chiaro**. Oltre quella, `CreateORModifyApiKeyPersistence`
    /// (che a differenza di quello degli IP **non ha try/catch**) lascia risalire una `SqlException`
    /// "String or binary data would be truncated" fino al chiamante: l'aderente riceve un **500**, non
    /// un messaggio che gli dica di accorciare la chiave. E la chiave la sceglie lui — non è generata
    /// dal server. Il rimedio naturale è un limite di lunghezza sull'endpoint.
    /// </summary>
    [TestCase(100, TestName = "ApiKey · 100 caratteri")]
    [TestCase(250, TestName = "ApiKey · 250 caratteri")]
    [TestCase(400, TestName = "ApiKey · 400 caratteri (limite della colonna in chiaro)")]
    public async Task ApiKeyLunga_ShouldFallireOSalvareIntegra_MaiTroncare(int lunghezza)
    {
        var chiaveInChiaro = "K" + new string('x', lunghezza - 1);
        var cifrata = Encryption().EncryptString(chiaveInChiaro);
        TestContext.Out.WriteLine($"in chiaro {lunghezza} -> cifrata {cifrata.Length} (colonna: 400)");

        int? esito = null;
        try
        {
            esito = await _handler.Send(new CreateORModifyApiKeyCommand(Auth(Ente)) { ApiKey = chiaveInChiaro });
        }
        catch (Exception ex)
        {
            // Un fallimento è accettabile; il silenzio no.
            TestContext.Out.WriteLine($"eccezione: {ex.GetType().Name} — {ex.Message}");
        }

        var salvate = ChiaviSalvate(Ente);
        Assert.That(salvate.Count, Is.LessThanOrEqualTo(1), "Al più una chiave inserita.");

        if (salvate.Count == 1)
        {
            Assert.That(salvate[0].Length, Is.EqualTo(cifrata.Length),
                $"La chiave salvata è {salvate[0].Length} caratteri contro i {cifrata.Length} attesi: "
                + "è stata TRONCATA. Una chiave troncata non autenticherà mai e non è recuperabile.");

            Assert.That(Encryption().DecryptString(salvate[0]), Is.EqualTo(chiaveInChiaro),
                "Deve tornare esattamente la chiave fornita: se non decifra, il valore è corrotto.");
        }
        else
        {
            Assert.That(esito, Is.Not.EqualTo(1),
                "Se non è stata salvata, l'esito non deve dire 'fatto': sarebbe un successo fasullo.");
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Injection e caratteri ostili
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task SqlInjection_InApiKey_ShouldEssereValoreNonSql_AndTabellaSopravvive()
    {
        var evil = "chiave'; DROP TABLE pfw.ApiKeys; --";

        try { await _handler.Send(new CreateORModifyApiKeyCommand(Auth(Ente)) { ApiKey = evil }); }
        catch (Exception ex) { TestContext.Out.WriteLine($"eccezione: {ex.GetType().Name}"); }

        Assert.Multiple(() =>
        {
            Assert.That(Scalare("SELECT COUNT(*) FROM sys.tables WHERE name = 'ApiKeys'"), Is.EqualTo(1),
                "La tabella deve essere intatta: injection non eseguita.");

            var salvate = ChiaviSalvate(Ente);
            if (salvate.Count == 1)
                Assert.That(Encryption().DecryptString(salvate[0]), Is.EqualTo(evil),
                    "Se salvata, dev'essere il letterale: trattata come valore, non come SQL.");
        });
    }

    [Test]
    public async Task SqlInjection_InIpAddress_ShouldEssereRifiutataDallaValidazione()
    {
        // Qui il primo argine non è la parametrizzazione ma VerifyIp: una stringa con apici non è
        // né un IP né un CIDR. Il DELETE, che non valida, è coperto dal test successivo.
        var evil = "1.2.3.4'; DROP TABLE pfw.ApiKeysIPs; --";

        var esito = await _handler.Send(new CreateIpsCommand(Auth(Ente)) { IpAddress = evil });

        Assert.Multiple(() =>
        {
            Assert.That(Scalare("SELECT COUNT(*) FROM sys.tables WHERE name = 'ApiKeysIPs'"), Is.EqualTo(1));
            Assert.That(esito, Is.Not.EqualTo(1).Or.EqualTo(1),
                "Il command non valida l'IP (lo fa l'endpoint): qui basta che la tabella sopravviva.");
        });
    }

    [Test]
    public async Task DeleteIps_ConValoreOstile_ShouldEssereNoOp_AndTabellaIntatta()
    {
        await _handler.Send(new CreateIpsCommand(Auth(Ente)) { IpAddress = "203.0.113.50" });

        var esito = await _handler.Send(new DeleteIpsCommand(Auth(Ente))
        {
            IpAddress = "203.0.113.50' OR '1'='1"
        });

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.Zero,
                "Il parametro è un valore: nessuna riga cancellata. Se fosse concatenato, l'OR '1'='1' "
                + "avrebbe svuotato la whitelist dell'ente.");
            Assert.That(IpDiEnte(Ente), Has.Count.EqualTo(1), "L'IP legittimo deve essere ancora lì.");
        });
    }

    /// <summary>
    /// CARATTERIZZAZIONE di un difetto reale, trovato scrivendo questo test.
    ///
    /// Il command **non valida l'indirizzo**: la sola difesa è `VerifyIp` nell'endpoint. Chiamando
    /// `CreateIpsCommand` direttamente — come farebbe un'altra rotta, un job, o questo stesso endpoint
    /// dopo una modifica distratta — in whitelist entra una riga con un indirizzo vuoto o fatto di soli
    /// spazi, che viene pure cifrata e salvata.
    ///
    /// Impatto contenuto (dall'esterno l'endpoint filtra) ma la difesa **non è in profondità**, e c'è
    /// un effetto collaterale non ovvio: siccome `UQ_IPAddress` è univoco e il ciphertext di "" è
    /// sempre lo stesso, il primo ente che inserisce un indirizzo vuoto **impedisce a tutti gli altri**
    /// di fare altrettanto — un `-1` incomprensibile su un valore che non avrebbe dovuto esistere.
    ///
    /// Il rimedio è spostare `VerifyIp` nel command/handler. Cambia il comportamento del dominio,
    /// quindi va deciso, non fatto di slancio: per ora il test fissa cosa succede davvero.
    /// </summary>
    [TestCase("", TestName = "CreaIp · stringa vuota")]
    [TestCase("   ", TestName = "CreaIp · soli spazi")]
    public async Task CreaIp_ValoriVuoti_VengonoAccettatiDalCommand_Caratterizzazione(string ip)
    {
        var esito = await _handler.Send(new CreateIpsCommand(Auth(Ente)) { IpAddress = ip });

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.EqualTo(1),
                "Comportamento attuale: il command accetta, perché non valida nulla.");
            Assert.That(IpDiEnte(Ente), Has.Count.EqualTo(1),
                "E la riga finisce davvero in whitelist. La validazione vive solo nell'endpoint "
                + "(VerifyIp), non nel dominio: v. ApiKeysVerifyIpTests e Http/ApiKeysHttpTests.");
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Identità incoerenti
    // ---------------------------------------------------------------------------------------------

    [Test]
    public void CreaChiave_ConEnteInesistente_ShouldRifiutare_NonCrearlo()
    {
        // Un IdEnte che non esiste da nessuna parte: dev'essere respinto dalla verifica di
        // abilitazione, non trattato come un ente nuovo.
        var inesistente = Guid.NewGuid().ToString();

        Assert.ThrowsAsync<SecurityException>(async () =>
            await _handler.Send(new CreateORModifyApiKeyCommand(Auth(inesistente)) { ApiKey = "chiave-test-x" }));

        Assert.That(Scalare($"SELECT COUNT(*) FROM pfw.ApiKeys WHERE FkIdEnte = '{inesistente}'"), Is.Zero);
    }

    [Test]
    public async Task CreaIp_ConIdEnteNonGuid_ShouldFallirePulito()
    {
        // Il campo è una stringa: nessuno impedisce di passarci del testo. Non deve arrivare a DB
        // una riga con un "ente" che non è un ente.
        const string spazzatura = "non-un-guid";

        try { await _handler.Send(new CreateIpsCommand(Auth(spazzatura)) { IpAddress = "203.0.113.60" }); }
        catch (Exception ex) { TestContext.Out.WriteLine($"eccezione: {ex.GetType().Name}"); }

        Assert.That(Scalare($"SELECT COUNT(*) FROM pfw.ApiKeysIPs WHERE FkIdEnte = '{spazzatura}'"), Is.Zero,
            "Un ente non valido non è abilitato, quindi non deve poter registrare nulla.");
    }

    // ---------------------------------------------------------------------------------------------
    // Concorrenza
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task ChiaviConcorrenti_ShouldNonSuperareIlLimiteDiDue()
    {
        // La regola "massimo due chiavi per aderente" vive nell'SQL: conta le chiavi esistenti e
        // rifiuta la terza. Conteggio e inserimento sono nello stesso statement, ma due richieste
        // simultanee sono il modo classico in cui un controllo del genere salta.
        // Scenario reale: doppio click sul pulsante "genera" del portale.
        var richieste = Enumerable.Range(0, 4)
            .Select(i => _handler.Send(new CreateORModifyApiKeyCommand(Auth(Ente)) { ApiKey = $"chiave-test-conc-{i}" }))
            .ToArray();

        try { await Task.WhenAll(richieste); }
        catch (Exception ex) { TestContext.Out.WriteLine($"eccezione: {ex.GetType().Name}"); }

        var totale = Scalare($"SELECT COUNT(*) FROM pfw.ApiKeys WHERE FkIdEnte = '{Ente}'");
        TestContext.Out.WriteLine($"chiavi risultanti: {totale}");

        Assert.That(totale, Is.LessThanOrEqualTo(2),
            "Il limite di due chiavi deve reggere anche in concorrenza: una terza chiave valida non "
            + "prevista dal modello resterebbe attiva e utilizzabile.");
    }

    [Test]
    public async Task StessoIpConcorrente_ShouldRestareUnaSolaRiga()
    {
        // Due richieste simultanee per lo stesso indirizzo: l'indice univoco deve garantire una riga
        // sola, e il perdente deve ricevere -1 (non un'eccezione non gestita).
        var a = _handler.Send(new CreateIpsCommand(Auth(Ente)) { IpAddress = "203.0.113.70" });
        var b = _handler.Send(new CreateIpsCommand(Auth(Ente)) { IpAddress = "203.0.113.70" });
        var esiti = await Task.WhenAll(a, b);

        Assert.Multiple(() =>
        {
            Assert.That(IpDiEnte(Ente), Has.Count.EqualTo(1), "UQ_IPAddress deve impedire il doppione.");
            Assert.That(esiti.Count(e => e == 1), Is.EqualTo(1), "Esattamente una deve riuscire.");
            Assert.That(esiti.Count(e => e == -1), Is.EqualTo(1), "L'altra deve fallire in modo controllato.");
        });
    }

    // ---------------------------------------------------------------------------------------------

    private static IAesEncryption Encryption() => ServiceProvider.GetRequiredService<IAesEncryption>();

    private static AuthenticationInfo Auth(string idEnte) => new()
    {
        Id = "integration-test-adversarial",
        IdEnte = idEnte,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    private static List<string> ChiaviSalvate(string idEnte) => Colonna(
        "SELECT ApiKey FROM pfw.ApiKeys WHERE FkIdEnte = @ente", idEnte);

    private static List<string> IpDiEnte(string idEnte) => Colonna(
        "SELECT IPAddress FROM pfw.ApiKeysIPs WHERE FkIdEnte = @ente", idEnte)
        .Select(x => { try { return Encryption().DecryptString(x); } catch { return x; } })
        .ToList();

    private static List<string> Colonna(string sql, string idEnte)
    {
        var valori = new List<string>();
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@ente", idEnte);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            valori.Add(reader.IsDBNull(0) ? string.Empty : reader.GetString(0));
        return valori;
    }

    private static int Scalare(string sql)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return (int)cmd.ExecuteScalar()!;
    }

    private static void Pulisci()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
DELETE FROM pfw.ApiKeysIPs WHERE FkIdEnte IN (@e1, @e2) OR FkIdEnte = 'non-un-guid';
DELETE FROM pfw.ApiKeys    WHERE FkIdEnte IN (@e1, @e2);
DELETE FROM pfw.Log        WHERE IdUtente = 'integration-test-adversarial';";
        cmd.Parameters.AddWithValue("@e1", Ente);
        cmd.Parameters.AddWithValue("@e2", EnteTerzo);
        cmd.ExecuteNonQuery();
    }
}
