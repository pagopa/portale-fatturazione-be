using System.Security;
using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Command di scrittura dell'area ApiKeys: chiavi delle Integration API e whitelist IP, che gli
/// aderenti gestiscono in autonomia dal portale. Era l'unica superficie di autenticazione senza
/// alcuna copertura, ed è quella dove un difetto significa accesso ai dati di un altro ente.
///
/// Le regole di business NON stanno nel C#: sono dentro l'SQL di CreateORModifyApiKeyPersistence
/// (massimo due chiavi per ente, ramo di rotazione) e dentro i due INDICI UNIVOCI di
/// tests/Data/api_keys.sql. Per questo servono test su DB, non unit test.
///
/// Sandbox: gli IP stanno nella rete 203.0.113.0/24 (TEST-NET-3, riservata alla documentazione), le
/// chiavi hanno un prefisso riconoscibile. Il cleanup agisce solo su quelli.
///
/// La logica pura di validazione IP è coperta a parte da ApiKeysVerifyIpTests (unit).
/// </summary>
public class ApiKeysCommandIntegrationTests
{
    private const string EnteAbilitato = "11111111-1111-1111-1111-111111111111";
    private const string EnteNonAbilitato = "22222222-2222-2222-2222-222222222222";
    private const string EnteTerzo = "33333333-3333-3333-3333-333333333333";
    private const string PrefissoIp = "203.0.113.";

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
    // Whitelist IP
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task CreaIp_Nuovo_ShouldReturn1_AndInserire()
    {
        var esito = await _handler.Send(new CreateIpsCommand(Auth(EnteAbilitato)) { IpAddress = $"{PrefissoIp}10" });

        Assert.That(esito, Is.EqualTo(1));
        Assert.That(IpDiEnte(EnteAbilitato), Is.EquivalentTo(new[] { $"{PrefissoIp}10" }));
    }

    [Test]
    public async Task CreaIp_PiuIndirizziPerLoStessoEnte_ShouldEssereConsentito()
    {
        await _handler.Send(new CreateIpsCommand(Auth(EnteAbilitato)) { IpAddress = $"{PrefissoIp}10" });
        var esito = await _handler.Send(new CreateIpsCommand(Auth(EnteAbilitato)) { IpAddress = $"{PrefissoIp}11" });

        Assert.That(esito, Is.EqualTo(1), "Un ente può avere più IP in whitelist: l'unicità è sull'indirizzo, non sull'ente.");
        Assert.That(IpDiEnte(EnteAbilitato), Has.Count.EqualTo(2));
    }

    [Test]
    public async Task CreaIp_Duplicato_StessoEnte_ShouldReturnMeno1()
    {
        await _handler.Send(new CreateIpsCommand(Auth(EnteAbilitato)) { IpAddress = $"{PrefissoIp}10" });
        var esito = await _handler.Send(new CreateIpsCommand(Auth(EnteAbilitato)) { IpAddress = $"{PrefissoIp}10" });

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.EqualTo(-1),
                "La violazione di UQ_IPAddress viene intercettata (SqlException 2601) e tradotta in -1, "
                + "non propagata come eccezione.");
            Assert.That(IpDiEnte(EnteAbilitato), Has.Count.EqualTo(1), "Nessun duplicato deve restare a DB.");
        });
    }

    [Test]
    public async Task CreaIp_StessoIndirizzoSuEnteDiverso_ShouldReturnMeno1_Finding()
    {
        // ATTENZIONE CONSEGUENZA NON OVVIA DELLO SCHEMA: UQ_IPAddress è univoco sulla SOLA colonna IPAddress,
        // non sulla coppia (FkIdEnte, IPAddress). Quindi il primo aderente che registra un indirizzo
        // impedisce a QUALSIASI altro di registrare lo stesso — scenario tutt'altro che teorico con
        // uscite NAT condivise o due enti dietro lo stesso gateway.
        // Il secondo riceve un -1 identico a quello di un duplicato proprio, quindi dal portale vede
        // "IP già presente" senza poter capire che appartiene a un altro ente.
        await _handler.Send(new CreateIpsCommand(Auth(EnteAbilitato)) { IpAddress = $"{PrefissoIp}20" });

        var esito = await _handler.Send(new CreateIpsCommand(Auth(EnteTerzo)) { IpAddress = $"{PrefissoIp}20" });

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.EqualTo(-1), "Comportamento attuale: l'indirizzo è occupato globalmente.");
            Assert.That(IpDiEnte(EnteTerzo), Is.Empty);
            Assert.That(IpDiEnte(EnteAbilitato), Has.Count.EqualTo(1), "Il primo ente mantiene il suo IP.");
        });
    }

    [Test]
    public async Task EliminaIp_Esistente_ShouldReturn1_AndRimuovereFisicamente()
    {
        await _handler.Send(new CreateIpsCommand(Auth(EnteAbilitato)) { IpAddress = $"{PrefissoIp}30" });

        var esito = await _handler.Send(new DeleteIpsCommand(Auth(EnteAbilitato)) { IpAddress = $"{PrefissoIp}30" });

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.EqualTo(1));
            Assert.That(IpDiEnte(EnteAbilitato), Is.Empty,
                "A differenza della whitelist fatture, qui la cancellazione è un DELETE fisico: nessuno storico della rimozione.");
        });
    }

    [Test]
    public async Task EliminaIp_Inesistente_ShouldReturn0()
    {
        var esito = await _handler.Send(new DeleteIpsCommand(Auth(EnteAbilitato)) { IpAddress = $"{PrefissoIp}99" });

        Assert.That(esito, Is.Zero, "Nessuna riga cancellata: 0, non un errore.");
    }

    [Test]
    public async Task EliminaIp_DiUnAltroEnte_ShouldReturn0_AndLasciarloIntatto()
    {
        // Isolamento fra aderenti: il DELETE filtra anche per FkIdEnte, quindi un ente non può
        // rimuovere l'IP di un altro nemmeno conoscendone l'indirizzo.
        await _handler.Send(new CreateIpsCommand(Auth(EnteAbilitato)) { IpAddress = $"{PrefissoIp}40" });

        var esito = await _handler.Send(new DeleteIpsCommand(Auth(EnteTerzo)) { IpAddress = $"{PrefissoIp}40" });

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.Zero);
            Assert.That(IpDiEnte(EnteAbilitato), Has.Count.EqualTo(1));
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Chiavi API
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task CreaChiave_EnteNonAbilitato_ShouldThrowSecurityException()
    {
        // pfw.EntiApiKeys è la lista degli enti abilitati alle Integration API (attiva=1). L'ente2 del
        // seed ha attiva=0: il command deve rifiutare PRIMA di scrivere qualsiasi cosa.
        var ex = Assert.ThrowsAsync<SecurityException>(async () =>
            await _handler.Send(new CreateORModifyApiKeyCommand(Auth(EnteNonAbilitato))
            {
                ApiKey = $"chiave-test-{Guid.NewGuid()}"
            }));

        Assert.That(ex!.Message, Does.Contain("non registrato").IgnoreCase);
    }

    [Test]
    public async Task CreaChiave_PrimaESeconda_ShouldEssereConsentite()
    {
        await _handler.Send(NuovaChiave());
        await _handler.Send(NuovaChiave());

        Assert.That(ChiaviDiEnte(EnteAbilitato), Is.EqualTo(2),
            "Il modello prevede due chiavi per aderente: primaria (obbligatoria) e secondaria (opzionale).");
    }

    [Test]
    public async Task CreaChiave_Terza_ShouldReturnMeno1_AndLasciarneDue()
    {
        // La regola "massimo due chiavi" NON è nel C#: è il primo ramo dell'SQL
        // (@ExistingMatch = 0 AND @KeyCount > 1 -> SELECT -1). Una pulizia di quella query la
        // farebbe sparire senza che nulla fallisca a compile-time.
        await _handler.Send(NuovaChiave());
        await _handler.Send(NuovaChiave());

        var esito = await _handler.Send(NuovaChiave());

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.EqualTo(-1), "La terza chiave dev'essere rifiutata.");
            Assert.That(ChiaviDiEnte(EnteAbilitato), Is.EqualTo(2), "E non deve lasciare residui.");
        });
    }

    [Test]
    public async Task CreaChiave_EsitoPositivo_RiportaLaScritturaSuLog_Caratterizzazione()
    {
        // CARATTERIZZAZIONE del valore di ritorno: l'handler, dopo l'upsert della chiave, scrive lo
        // storico su pfw.Log e RESTITUISCE LE RIGHE DI QUELL'INSERT, non quelle della chiave. Quindi
        // un successo vale sempre 1, qualunque cosa abbia fatto l'upsert (insert o update).
        var logPrima = RigheLog(EnteAbilitato);

        var esito = await _handler.Send(NuovaChiave());

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.EqualTo(1));
            Assert.That(RigheLog(EnteAbilitato), Is.EqualTo(logPrima + 1),
                "L'operazione è tracciata: se il log non viene scritto l'handler fa rollback anche della chiave.");
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Helper
    // ---------------------------------------------------------------------------------------------

    private static CreateORModifyApiKeyCommand NuovaChiave() =>
        new(Auth(EnteAbilitato)) { ApiKey = $"chiave-test-{Guid.NewGuid()}" };

    private static AuthenticationInfo Auth(string idEnte) => new()
    {
        Id = "integration-test-apikeys",
        IdEnte = idEnte,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    /// <summary>
    /// Gli IP sono salvati CIFRATI (v. tests/Data/api_keys.sql): per confrontarli si ricifra il valore
    /// atteso con la stessa chiave del container DI dei test. Funziona perché AesEncryption usa un IV
    /// a zero ed è quindi deterministico.
    /// </summary>
    private static List<string> IpDiEnte(string idEnte)
    {
        var encryption = ServiceProvider.GetRequiredService<IAesEncryption>();
        var ips = new List<string>();

        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT IPAddress FROM pfw.ApiKeysIPs WHERE FkIdEnte = @ente";
        cmd.Parameters.AddWithValue("@ente", idEnte);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            ips.Add(encryption.DecryptString(reader.GetString(0)));

        return ips;
    }

    private static int ChiaviDiEnte(string idEnte) =>
        Scalare("SELECT COUNT(*) FROM pfw.ApiKeys WHERE FkIdEnte = @ente", idEnte);

    private static int RigheLog(string idEnte) =>
        Scalare("SELECT COUNT(*) FROM pfw.Log WHERE FkIdEnte = @ente", idEnte);

    private static int Scalare(string sql, string idEnte)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@ente", idEnte);
        return (int)cmd.ExecuteScalar()!;
    }

    private static void Pulisci()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        // Cleanup per ENTE, non per indirizzo: i valori a DB sono cifrati, quindi una WHERE sul
        // testo in chiaro non cancellerebbe nulla (ed e' esattamente cosi' che questi test hanno
        // fallito la prima volta, lasciando residui che facevano scattare UQ_IPAddress).
        cmd.CommandText = @"
DELETE FROM pfw.ApiKeysIPs WHERE FkIdEnte IN (@e1, @e2, @e3);
DELETE FROM pfw.ApiKeys    WHERE FkIdEnte IN (@e1, @e2, @e3);
DELETE FROM pfw.Log        WHERE IdUtente = 'integration-test-apikeys';";
        cmd.Parameters.AddWithValue("@e1", EnteAbilitato);
        cmd.Parameters.AddWithValue("@e2", EnteNonAbilitato);
        cmd.Parameters.AddWithValue("@e3", EnteTerzo);
        cmd.ExecuteNonQuery();
    }
}
