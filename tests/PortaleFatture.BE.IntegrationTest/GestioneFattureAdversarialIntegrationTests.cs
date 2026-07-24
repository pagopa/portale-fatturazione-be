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
            var r = await Send("POSTICIPA", Ente1, 2026, 7, evil, 1001);
            Assert.That(r, Is.EqualTo(0), "Tipologia malevola: nessun match, no-op (nessuna injection).");
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
