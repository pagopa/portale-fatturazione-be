using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Test ADVERSARIAL end-to-end su `AzioneContestazioneQueryGetByIdNotificaHandler`. Qui il bersaglio
/// non e' la matrice dei permessi (quella e' coperta a unit) ma le **due letture** che la precedono, e
/// in particolare una scelta che accomuna `NotificaQueryGetByIdPersistence` e
/// `ContestazioneQueryGetByIdNotificaPersistence`: entrambe avvolgono la query in un
/// `try { … } catch { return null; }`.
///
/// Quel catch senza filtro non distingue "riga assente" da "qualcosa e' andato storto", e l'handler
/// traduce il null in `DomainException("Non esiste la notifica con codice: …")`. Ogni anomalia — di
/// dato, di schema o di connessione — arriva quindi all'utente come *"la notifica non esiste"*.
/// </summary>
public class AzioneContestazioneAdversarialIntegrationTests
{
    private const string Ente = "11111111-1111-1111-1111-111111111111";
    private const string Duplicato = "EVT-DUP-ADV";

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
    // Il catch che nasconde le anomalie
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// ATTENZIONE DIFETTO. `pfd.Notifiche` **non ha un vincolo di unicita' su `event_id`** (la colonna e'
    /// `nvarchar(400) NULL`, senza PK ne' indice univoco — v. il DDL nel seed), quindi due righe con
    /// lo stesso id sono fisicamente possibili: e' esattamente cio' che una reingestione parziale
    /// della pipeline puo' produrre.
    ///
    /// La persistence usa `SingleAsync`, che con 2 righe solleva; il `catch` lo inghiotte e
    /// restituisce null; l'handler conclude che **la notifica non esiste**. L'operatore vede sparire
    /// una notifica che a database c'e' due volte, e nei log resta solo "Non esiste la notifica" — la
    /// causa reale non compare da nessuna parte.
    ///
    /// E' la stessa forma del 500 su `vwRelDettaglio` documentata in `docs/viste-endpoint.md`, ma con
    /// esito opposto e peggiore: li' l'anomalia era rumorosa, qui e' muta.
    /// </summary>
    [Ignore("DIFETTO APERTO — due righe con lo stesso event_id (reingestione parziale della pipeline) "
        + "vengono riportate all'utente come 'notifica inesistente': SingleAsync solleva e il "
        + "catch senza filtro di NotificaQueryGetByIdPersistence lo traduce in null. La causa reale "
        + "non finisce nemmeno nei log. Il test asserisce il comportamento CORRETTO (un errore che "
        + "dica cosa e' successo): togliere questo attributo quando il catch verra' ristretto alle "
        + "sole eccezioni attese, o quando event_id avra' un vincolo di unicita'.")]
    [Test]
    public void NotificaDuplicata_ShouldSegnalareUnAnomalia_NonDireCheNonEsiste()
    {
        InserisciNotifica(Duplicato);
        InserisciNotifica(Duplicato);

        var ex = Assert.ThrowsAsync<DomainException>(async () => await Esegui(Duplicato));

        Assert.That(ex!.Message, Does.Not.Contain("Non esiste"),
            "Comportamento attuale: due righe con lo stesso event_id vengono riportate come "
            + "'notifica inesistente'. Il catch senza filtro delle due persistence confonde "
            + "l'anomalia di dato con l'assenza del dato. Atteso: un errore che dica cosa e' successo.");
    }

    /// <summary>
    /// Contro-prova che isola la causa del test precedente: con **una sola** riga lo stesso id
    /// funziona. E' il duplicato a rompere, non l'id inventato dal test.
    /// </summary>
    [Test]
    public async Task StessaNotificaNonDuplicata_ShouldFunzionare()
    {
        InserisciNotifica(Duplicato);

        var esito = await Esegui(Duplicato);

        Assert.That(esito!.Notifica!.IdNotifica, Is.EqualTo(Duplicato));
    }

    // ---------------------------------------------------------------------------------------------
    // L'identificativo, che arriva dalla rotta
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// L'id viaggia fino a `WHERE n.event_id=@IdNotifica` come parametro Dapper. Valori ostili devono
    /// restare valori: nessuna tabella toccata, e l'esito e' il "non esiste" ordinario.
    /// </summary>
    [TestCase("EVT-3002'; DROP TABLE pfd.Notifiche; --", TestName = "id · tentativo di DROP")]
    [TestCase("' OR '1'='1", TestName = "id · condizione sempre vera")]
    public void IdOstile_ShouldEssereTrattatoComeValore(string ostile)
    {
        Assert.ThrowsAsync<DomainException>(async () => await Esegui(ostile));

        Assert.That(Scalare("SELECT COUNT(*) FROM sys.tables WHERE name = 'Notifiche'"), Is.EqualTo(1),
            "La tabella deve essere intatta.");
    }

    /// <summary>
    /// ATTENZIONE Il caso `' OR '1'='1` merita una nota: se l'id fosse concatenato invece che parametrizzato,
    /// la `WHERE` diventerebbe sempre vera e `SingleAsync` troverebbe **piu' righe**, quindi
    /// solleverebbe — e il `catch` lo tradurrebbe di nuovo in "non esiste". L'esito osservabile
    /// sarebbe identico a quello corretto: la parametrizzazione va verificata sulla tabella, non
    /// sull'eccezione. E' quello che fa il test sopra.
    /// </summary>
    [TestCase("", TestName = "id · stringa vuota")]
    [TestCase("   ", TestName = "id · soli spazi")]
    public void IdVuoto_ShouldRisultareInesistente(string id)
        => Assert.ThrowsAsync<DomainException>(async () => await Esegui(id));

    [Test]
    public void IdNullo_ShouldRisultareInesistente()
        => Assert.ThrowsAsync<DomainException>(async () => await Esegui(null));

    [Test]
    public void IdPiuLungoDellaColonna_ShouldRisultareInesistente()
    {
        // La colonna e' nvarchar(400): un id piu' lungo non deve produrre un errore in una SELECT,
        // solo nessun risultato.
        Assert.ThrowsAsync<DomainException>(async () => await Esegui(new string('X', 5000)));
    }

    // ---------------------------------------------------------------------------------------------
    // Notifica orfana: gli INNER JOIN della query
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// La query parte da `pfd.Notifiche` ma fa **INNER JOIN** su `pfd.Enti` e `pfd.Contratti`: una
    /// notifica il cui ente non e' (ancora) censito — scenario reale, visto che l'onboarding SelfCare
    /// arriva con un batch giornaliero — non produce zero righe "perche' l'ente manca", produce lo
    /// stesso "la notifica non esiste".
    ///
    /// E' un limite noto piu' che un difetto, ma vale la pena averlo scritto: e' la prima ipotesi da
    /// verificare quando una notifica risulta introvabile pur essendo a database.
    /// </summary>
    [Test]
    public void NotificaDiEnteNonCensito_ShouldRisultareInesistente()
    {
        InserisciNotifica("EVT-ORFANA-ADV", idEnte: "00000000-0000-0000-0000-0000000000ff");

        var ex = Assert.ThrowsAsync<DomainException>(async () => await Esegui("EVT-ORFANA-ADV"));

        Assert.That(ex!.Message, Does.Contain("Non esiste la notifica"),
            "L'INNER JOIN su Enti la scarta: indistinguibile da una notifica assente.");
    }

    // ---------------------------------------------------------------------------------------------

    private async Task<AzioneNotificaDto?> Esegui(string? idNotifica)
        => await _handler.Send(new AzioneContestazioneQueryGetByIdNotifica(
            new AuthenticationInfo
            {
                Id = "integration-test-azione-adv",
                IdEnte = Ente,
                Prodotto = "prod-pn",
                Profilo = "PA",
                Ruolo = Ruolo.ADMIN,
                IdTipoContratto = 1
            },
            idNotifica));

    private static void InserisciNotifica(string eventId, string? idEnte = null) => Esegui(@"
INSERT INTO pfd.Notifiche
 (contract_id, tax_code, vat_number, zip_code, number_of_pages, g_envelope_weight, cost_eurocent,
  timeline_category, paper_product_type, event_id, iun, notification_sent_at,
  internal_organization_id, event_timestamp, recipient_index, recipient_type, recipient_id,
  [year], [month], daily, item_code, notification_request_id, recipient_tax_id, notificationtype,
  Recapitista, Consolidatore, TipologiaFattura, Fatturabile)
VALUES
 ('TOKEN-E1', 'ADV', 'ADV', '00100', 1, '10', 100, 'SEND_ANALOG_DOMICILE', 'AR',
  @evt, 'IUN-ADV', '2026-03-02', @ente, '2026-03-05T10:00:00', '0', 'PF', 'REC-ADV',
  2026, 3, '2026-03-05', 'IC-ADV', 'NRQ-ADV', 'TAX-ADV', 'AnalogicoARNazionali',
  NULL, NULL, NULL, 0);",
        ("@evt", eventId),
        ("@ente", idEnte ?? Ente));

    private static void Pulisci()
        => Esegui("DELETE FROM pfd.Notifiche WHERE event_id LIKE 'EVT-%-ADV' OR event_id = @dup;",
            ("@dup", Duplicato));

    private static void Esegui(string sql, params (string nome, object valore)[] parametri)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (nome, valore) in parametri)
            cmd.Parameters.AddWithValue(nome, valore);
        cmd.ExecuteNonQuery();
    }

    private static int Scalare(string sql)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
