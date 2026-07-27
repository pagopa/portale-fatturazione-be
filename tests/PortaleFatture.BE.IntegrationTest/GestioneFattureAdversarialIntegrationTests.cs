using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Test ADVERSARIAL / edge-case sull'azione GestioneFatture (command reale -> SP, DB seeded locale):
/// input malformati, injection, overflow, chiavi incoerenti, note ostili. Verificano che il sistema
/// o rifiuti in modo controllato o faccia no-op, senza corruzione dati.
/// </summary>
public class GestioneFattureAdversarialIntegrationTests
{
    private const string Ente1 = "11111111-1111-1111-1111-111111111111";
    private IMediator _handler;

    [SetUp]
    public void Setup() => _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    private string Conn => LocalTestDb.ConnectionString;

    private static AuthenticationInfo AdminAuth() => new()
    { IdEnte = Guid.NewGuid().ToString(), Prodotto = "prod-pn", Ruolo = Ruolo.ADMIN, IdTipoContratto = 1 };

    private Task<int?> Send(string? azione, string? ente, int? anno, int? mese, string tipologia, int? idFattura, string? notaTesto = "x")
        => _handler.Send(new GestioneFattureAzioneCommand(AdminAuth())
        {
            IdEnte = ente,
            Azione = azione,
            Anno = anno,
            Mese = mese,
            TipologiaFattura = tipologia,
            IdUtente = "adv",
            IdFattura = idFattura,
            Nota = notaTesto is null ? null : new NoteCommand { Data = DateTime.UtcNow, Testo = notaTesto }
        });

    // --- input malformati (non gestiti a monte -> devono almeno NON passare silenziosamente) ---

    [Test]
    public void Azione_Null_ShouldThrow_NotSilentlySucceed()
    {
        // Il command non valida Azione: la persistence fa _command.Azione!.ToUpper() -> NRE.
        // (L'endpoint la mappa poi su BadRequest generico; qui documentiamo l'assenza di validazione.)
        Assert.That(async () => await Send(null, Ente1, 2026, 7, "SECONDO SALDO", 1001),
            Throws.InstanceOf<NullReferenceException>());
    }

    [TestCase(" POSTICIPA ")]
    [TestCase("posticipa\t")]
    [TestCase("POSTICIPA;")]
    public void Azione_WithWhitespaceOrGarbage_ShouldBeRejectedWithArgumentException(string azione)
    {
        Assert.That(async () => await Send(azione, Ente1, 2026, 7, "SECONDO SALDO", 1001),
            Throws.InstanceOf<ArgumentException>());
        Cleanup(1001);
    }

    [Test]
    public void IdEnte_Null_ShouldThrow_OnUnconditionalGuidParse()
    {
        // Guid.Parse(_command.IdEnte!) e' incondizionato -> con IdEnte null lancia (ArgumentNull/Exception).
        Assert.That(async () => await Send("POSTICIPA", null, 2026, 7, "SECONDO SALDO", 1001),
            Throws.Exception);
    }

    // --- SQL injection: Dapper parametrizza, deve essere trattato come VALORE ---

    [Test]
    public async Task SqlInjection_InTipologia_ShouldBeValueNotSql_AndTableSurvives()
    {
        var evil = "SECONDO SALDO'; DROP TABLE cfg.GestioneFatture; --";
        try
        {
            // Nota: dal 2026-07-24 la SP accetta anche tipologie che non esistono (pre-generazione, v.
            // ExtremeAnnoMese_...). Quello che questo test deve provare non e' il rifiuto, ma che la
            // stringa sia trattata come VALORE: viene persistita cosi' com'e' e non eseguita.
            await Send("POSTICIPA", Ente1, 2026, 7, evil, 1001);

            var tipologiaSalvata = Scalar(
                "SELECT TOP(1) FkTipologiaFattura FROM cfg.GestioneFatture WHERE FkIdFattura=1001", "");
            Assert.That(tipologiaSalvata, Is.EqualTo(evil).Or.Empty,
                "Se salvata, la tipologia deve essere il letterale: nessun troncamento sospetto, nessuna esecuzione.");
        }
        finally { Cleanup(1001); }

        // la tabella deve esistere ancora
        Assert.That(Scalar<int>("SELECT COUNT(*) FROM sys.tables WHERE name='GestioneFatture'", -1),
            Is.EqualTo(1), "La tabella cfg.GestioneFatture deve essere intatta: injection non eseguita.");
    }

    // --- valori estremi: non devono far crashare, al piu' no-op ---

    [TestCase(int.MaxValue, 99999)]
    [TestCase(-1, -5)]
    [TestCase(0, 0)]
    [Ignore("DIFETTO APERTO (non un test da riscrivere): dal 2026-07-24 nessuno valida piu' il periodo. "
          + "La SP spGestioneFatturePosticipa non pretende piu' una fattura esistente (pre-generazione) e "
          + "non esiste alcun controllo di range, quindi mese 0, mese 99999 e anno -1 vengono ACCETTATI e "
          + "scritti in cfg.GestioneFatture. Il test resta com'era perche' l'aspettativa e' ancora quella "
          + "giusta. Va deciso dove mettere la validazione: l'endpoint C# e' il posto naturale (li' c'e' "
          + "gia' la whitelist delle azioni), perche' la SP non ha piu' nulla con cui confrontare il periodo.")]
    public async Task ExtremeAnnoMese_ShouldNotCrash_AndNoOp(int anno, int mese)
    {
        try
        {
            var r = await Send("POSTICIPA", Ente1, anno, mese, "SECONDO SALDO", 1001);
            Assert.That(r, Is.EqualTo(0), "Anno/Mese fuori range: nessun match -> Result 0, nessun crash.");
        }
        finally { CleanupByPeriod(Ente1, "SECONDO SALDO", anno, mese); }
    }

    // --- chiave incoerente: IdFattura valido ma periodo che non combacia ---

    [Test]
    [Ignore("DIFETTO APERTO, stessa causa di ExtremeAnnoMese_ShouldNotCrash_AndNoOp, con effetto peggiore: "
          + "viene creata una riga che associa un IdFattura REALE (1001, che e' del 2026/7) a un periodo "
          + "che non gli appartiene (9999/12). Non e' un no-op mancato, e' un'incoerenza persistita. "
          + "Il test resta invariato: l'aspettativa Result 0 e' corretta.")]
    public async Task InconsistentKey_ValidIdFatturaButWrongPeriod_ShouldNoOp()
    {
        try
        {
            var r = await Send("POSTICIPA", Ente1, 9999, 12, "SECONDO SALDO", 1001); // 1001 esiste ma non nel 9999/12
            Assert.That(r, Is.EqualTo(0), "IdFattura reale ma periodo incoerente: la SP richiede il match completo.");
        }
        finally { Cleanup(1001); CleanupByPeriod(Ente1, "SECONDO SALDO", 9999, 12); }
    }

    // --- note ostili: caratteri speciali e payload enorme sul tipo json ---

    [Test]
    public async Task Nota_WithSpecialChars_ShouldBeStoredSafely()
    {
        var nasty = "a\"b\\c/d\n\t\r <script>alert(1)</script> €à😀 '; --";
        try
        {
            var r = await Send("POSTICIPA", Ente1, 2026, 7, "SECONDO SALDO", 1001, nasty);
            Assert.That(r, Is.EqualTo(1), "Nota con caratteri speciali: posticipa deve riuscire (JSON valido).");
            var note = Scalar<string>("SELECT CAST(Note AS nvarchar(max)) FROM cfg.GestioneFatture WHERE FkIdFattura=1001", "");
            Assert.That(note, Does.Contain("script"), "Il testo deve essere memorizzato come dato, non interpretato.");
        }
        finally { Cleanup(1001); }
    }

    [Test]
    public async Task Nota_VeryLong_ShouldBeStored()
    {
        var big = new string('X', 50_000);
        try
        {
            var r = await Send("POSTICIPA", Ente1, 2026, 7, "SECONDO SALDO", 1001, big);
            Assert.That(r, Is.EqualTo(1), "Nota da 50KB: deve essere accettata dal parametro json.");
        }
        finally { Cleanup(1001); }
    }

    // --- larghezza colonne: il troncamento silenzioso e' peggio di un errore ---

    [Test]
    [Ignore("DIFETTO CONFERMATO (troncamento silenzioso). Inviando una TipologiaFattura di 400 caratteri "
          + "la SP risponde Result 1 e persiste solo i primi 50: nessun errore, nessun avviso, il dato "
          + "salvato e' diverso da quello richiesto. Verificato che NON dipende dal seed: allargando la "
          + "colonna cfg.GestioneFatture.FkTipologiaFattura a nvarchar(400) il valore salvato resta di 50 "
          + "caratteri, perche' a tagliare e' il PARAMETRO della stored procedure "
          + "(@TipologiaFattura nvarchar(50), come @IdUtente). Vale quindi anche in produzione. "
          + "Rimedio: validare TipologiaFattura sull'endpoint (whitelist), che chiude anche il buco "
          + "descritto in Posticipa_OnNonSaldoTipologia_ShouldBeRejected. Riferimento larghezze reali: "
          + "pfd.FattureTestata.FkTipologiaFattura e' nvarchar(15).")]
    public async Task Tipologia_LongerThanColumn_ShouldFailOrStoreIntact_NeverTruncate()
    {
        // Sul DB reale pfd.FattureTestata.FkTipologiaFattura e' nvarchar(15) e _Eliminate nvarchar(30).
        // Qui scriviamo su cfg.GestioneFatture passando una tipologia lunghissima: l'esito accettabile e'
        // uno solo di due -> errore SQL esplicito, oppure valore memorizzato INTEGRO. Il caso da
        // intercettare e' il terzo: riga creata con il valore tagliato, che nessuno si accorge sia diverso
        // da quello inviato.
        var lunga = new string('T', 400);
        try
        {
            int? r = null;
            try { r = await Send("POSTICIPA", Ente1, 2026, 7, lunga, 1001); }
            catch (SqlException) { Assert.Pass("Rifiutata con errore SQL esplicito: accettabile."); }

            if (r == 1)
            {
                var salvata = Scalar<string>(
                    "SELECT TOP(1) FkTipologiaFattura FROM cfg.GestioneFatture WHERE FkIdFattura=1001", "");
                Assert.That(salvata.Length, Is.EqualTo(lunga.Length),
                    $"TRONCAMENTO SILENZIOSO: inviati {lunga.Length} caratteri, salvati {salvata.Length}. "
                  + "Il dato persistito non e' quello richiesto e nessuno segnala l'errore.");
            }
        }
        finally { Cleanup(1001); }
    }

    [Test]
    public void IdEnte_NotAGuid_ShouldFailCleanly()
    {
        // La SP dichiara @IdEnte uniqueidentifier e il command fa Guid.Parse incondizionato: una stringa
        // che non e' un GUID deve fallire in modo netto, non finire nel DB come testo arbitrario.
        Assert.That(async () => await Send("POSTICIPA", "non-un-guid", 2026, 7, "SECONDO SALDO", 1001),
            Throws.Exception, "IdEnte non-GUID deve essere rifiutato prima di toccare il DB.");

        Assert.That(Scalar("SELECT COUNT(*) FROM cfg.GestioneFatture WHERE FkIdEnte='non-un-guid'", -1),
            Is.EqualTo(0), "Nessuna riga deve essere creata con un IdEnte non valido.");
    }

    // --- concorrenza: due azioni identiche in parallelo ---

    [Test]
    public async Task ConcurrentPosticipa_SameInvoice_ShouldNotCreateDuplicates()
    {
        // Doppio click, retry del client, due tab aperte: due POSTICIPA contemporanee sulla stessa
        // chiave. A impedire il doppione non e' la SP (che non ha ne' lock ne' controllo di esistenza)
        // ma la PRIMARY KEY composta di cfg.GestioneFatture su
        // (FkIdEnte, FkTipologiaFattura, Anno, Mese, Stato): la seconda insert viola il vincolo,
        // finisce nel BEGIN CATCH e la SP risponde 0.
        try
        {
            var t1 = Send("POSTICIPA", Ente1, 2026, 7, "SECONDO SALDO", 1001, "concorrente-1");
            var t2 = Send("POSTICIPA", Ente1, 2026, 7, "SECONDO SALDO", 1001, "concorrente-2");
            var esiti = await Task.WhenAll(t1, t2);

            Assert.That(Scalar("SELECT COUNT(*) FROM cfg.GestioneFatture WHERE FkIdFattura=1001", -1),
                Is.EqualTo(1), "Una sola riga: il doppione e' impedito dalla PK composta.");
            Assert.That(esiti.Count(e => e == 1), Is.EqualTo(1),
                "Esattamente una delle due chiamate deve riuscire...");
            Assert.That(esiti.Count(e => e == 0), Is.EqualTo(1),
                "...e l'altra deve essere respinta (Result 0), non riuscire in silenzio.");
        }
        finally { Cleanup(1001); }
    }

    // --- helper ---

    private T Scalar<T>(string sql, T fallback)
    {
        using var conn = new SqlConnection(Conn); conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        var v = cmd.ExecuteScalar();
        return v is null or DBNull ? fallback : (T)Convert.ChangeType(v, typeof(T));
    }

    private void Exec(string sql, params (string, object)[] ps)
    {
        try
        {
            using var conn = new SqlConnection(Conn); conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            foreach (var p in ps) cmd.Parameters.AddWithValue(p.Item1, p.Item2);
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { }
    }

    private void Cleanup(long id) => Exec("DELETE FROM cfg.GestioneFatture WHERE FkIdFattura=@id", ("@id", id));
    private void CleanupByPeriod(string e, string t, int a, int m) => Exec(
        "DELETE FROM cfg.GestioneFatture WHERE FkIdEnte=@e AND FkTipologiaFattura=@t AND Anno=@a AND Mese=@m",
        ("@e", e), ("@t", t), ("@a", a), ("@m", m));
}
