using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Test di CONFORMITA' AI REQUISITI di Gestione Fatture (PF-672), contro il DB seeded locale.
/// Copre: vincoli tipologia x azione, macchina a stati (flowchart), e due caratterizzazioni che
/// documentano scostamenti dal requisito (posticipa pre-generazione; modello Nota multipla).
///
/// Seed FattureTestata usati (da tests/Data/gestione_fatture.sql):
///  1001 SECONDO SALDO / ente1    | 1002 PRIMO SALDO / ente2 (non INPS)
///  2001 ANTICIPO / ente3         | 2002 ACCONTO / ente3
///  3001 PRIMO SALDO / ente INPS (53b40136...)
/// </summary>
public class GestioneFattureRequisitiIntegrationTests
{
    private const string Ente1 = "11111111-1111-1111-1111-111111111111";
    private const string Ente2 = "22222222-2222-2222-2222-222222222222";
    private const string Ente3 = "33333333-3333-3333-3333-333333333333";
    private const string EnteInps = "53b40136-65f2-424b-acfb-7fae17e35c60";

    private IMediator _handler;

    [SetUp]
    public void Setup() => _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);

    private string Conn => LocalTestDb.ConnectionString;

    private static AuthenticationInfo AdminAuth() => new()
    { IdEnte = Guid.NewGuid().ToString(), Prodotto = "prod-pn", Ruolo = Ruolo.ADMIN, IdTipoContratto = 1 };

    // ---------- 1) VINCOLI TIPOLOGIA x AZIONE ----------

    [TestCase(2001, "ANTICIPO", Ente3)]  // ANTICIPO non posticipabile
    [TestCase(2002, "ACCONTO", Ente3)]   // ACCONTO non posticipabile
    public async Task Posticipa_OnNonSaldoTipologia_ShouldBeRejected(int idFattura, string tipologia, string ente)
    {
        try
        {
            var r = await Send("POSTICIPA", idFattura, 2026, tipologia == "ANTICIPO" || tipologia == "ACCONTO" ? 5 : 7, ente, tipologia);
            Assert.That(r, Is.EqualTo(0), $"POSTICIPA su {tipologia} doveva essere rifiutata (Result 0).");
        }
        finally { Cleanup(idFattura); }
    }

    [Test]
    public async Task Elimina_OnSecondoSaldo_ShouldBeRejected()
    {
        try
        {
            var r = await Send("ELIMINA", 1001, 2026, 7, Ente1, "SECONDO SALDO");
            Assert.That(r, Is.EqualTo(0), "ELIMINA su SECONDO SALDO doveva essere rifiutata.");
        }
        finally { Cleanup(1001); }
    }

    [Test]
    public async Task Elimina_OnPrimoSaldoNonInps_ShouldBeRejected()
    {
        try
        {
            var r = await Send("ELIMINA", 1002, 2026, 6, Ente2, "PRIMO SALDO");
            Assert.That(r, Is.EqualTo(0), "ELIMINA su PRIMO SALDO non-INPS doveva essere rifiutata.");
        }
        finally { Cleanup(1002); }
    }

    [TestCase(2002, "ACCONTO", Ente3, 5)]           // ACCONTO eliminabile
    [TestCase(3001, "PRIMO SALDO", EnteInps, 6)]    // PRIMO SALDO INPS: eccezione ammessa
    public async Task Elimina_OnEliminableTipologia_ShouldSucceed(int idFattura, string tipologia, string ente, int mese)
    {
        var seed = SnapshotFattura(idFattura);
        try
        {
            var r = await Send("ELIMINA", idFattura, 2026, mese, ente, tipologia);
            Assert.That(r, Is.EqualTo(1), $"ELIMINA su {tipologia} ({ente}) doveva riuscire (Result 1).");
            Assert.That(ReadStato(idFattura), Is.EqualTo(3), "Stato atteso = 3 (ELIMINATA).");
        }
        finally { RestoreEliminata(seed); }
    }

    // ---------- 2) MACCHINA A STATI (flowchart) ----------

    [Test]
    public async Task Ripristina_OnAlreadyRipristinata_ShouldBeRejected()
    {
        try
        {
            Assert.That(await Send("POSTICIPA", 1001, 2026, 7, Ente1, "SECONDO SALDO"), Is.EqualTo(1));
            Assert.That(await Send("RIPRISTINA", 1001, 2026, 7, Ente1, "SECONDO SALDO"), Is.EqualTo(1));
            var second = await Send("RIPRISTINA", 1001, 2026, 7, Ente1, "SECONDO SALDO");
            Assert.That(second, Is.EqualTo(0), "RIPRISTINA su una gia' RIPRISTINATA (Stato=1) va rifiutata.");
        }
        finally { Cleanup(1001); }
    }

    [Test]
    public async Task Ripristina_OnCancellata_ShouldBeRejected()
    {
        try
        {
            Assert.That(await Send("POSTICIPA", 1001, 2026, 7, Ente1, "SECONDO SALDO"), Is.EqualTo(1));
            Assert.That(await Send("CANCELLA", 1001, 2026, 7, Ente1, "SECONDO SALDO"), Is.EqualTo(1));
            Assert.That(ReadStato(1001), Is.EqualTo(2));
            var r = await Send("RIPRISTINA", 1001, 2026, 7, Ente1, "SECONDO SALDO");
            Assert.That(r, Is.EqualTo(0), "RIPRISTINA su una CANCELLATA (Stato=2) va rifiutata.");
        }
        finally { Cleanup(1001); }
    }

    [Test]
    public async Task Cancella_OnEliminata_ShouldSucceed_PerRF06()
    {
        // RF06: una fattura pre-ELIMINATA puo' essere CANCELLATA (rimedio a errore admin).
        var seed = SnapshotFattura(2002);
        try
        {
            Assert.That(await Send("ELIMINA", 2002, 2026, 5, Ente3, "ACCONTO"), Is.EqualTo(1));
            Assert.That(ReadStato(2002), Is.EqualTo(3));
            var r = await Send("CANCELLA", 2002, 2026, 5, Ente3, "ACCONTO");
            Assert.That(r, Is.EqualTo(1), "CANCELLA su una ELIMINATA (Stato=3) deve riuscire (RF06).");
            Assert.That(ReadStato(2002), Is.EqualTo(2), "Stato atteso = 2 (CANCELLATA).");
        }
        finally { RestoreEliminata(seed); }
    }

    // ---------- 3) CARATTERIZZAZIONE: posticipa PRE-GENERAZIONE (scostamento dal Q&A) ----------

    [Test]
    [Ignore("ACCETTAZIONE requisito aggiornato: la posticipa e' ammessa anche su fatture NON ancora esistenti (pre-generazione, chiave Anno/Mese/Ente/Tipologia). La SP attuale richiede la fattura in FattureTestata -> da adeguare. Riabilitare dopo il fix SP.")]
    public async Task Posticipa_PreGeneration_NoInvoiceYet_ShouldSucceed_ByPeriod()
    {
        // Requisito AGGIORNATO: l'admin puo' posticipare un periodo (Ente/Tipologia/Anno/Mese) anche se la
        // fattura non esiste ancora in FattureTestata (PAC in attesa firma REL). Deve creare la riga
        // cfg.GestioneFatture con Stato=0 (POSTICIPATA) e FkIdFattura NULL. Criterio di accettazione del fix SP.
        try
        {
            var r = await Send("POSTICIPA", idFattura: null, anno: 1999, mese: 1, ente: Ente1, tipologia: "SECONDO SALDO");
            Assert.That(r, Is.EqualTo(1), "Posticipa pre-generazione deve riuscire (Result 1).");
            Assert.That(ReadStatoByPeriod(Ente1, "SECONDO SALDO", 1999, 1), Is.EqualTo(0),
                "Deve essere creata la riga POSTICIPATA (Stato=0) per periodo, senza fattura esistente.");
        }
        finally { CleanupByPeriod(Ente1, "SECONDO SALDO", 1999, 1); }
    }

    // ---------- 4) CARATTERIZZAZIONE: Nota multipla (scostamento dal modello a array) ----------

    [Test]
    public async Task Note_AfterPosticipa_ShouldBePersistedAndReadable()
    {
        // Il command manda una nota singola { Data, Testo }; la SP la gestisce nel campo json Note.
        // Verifica neutra: la nota risulta persistita e rileggibile (round-trip del tipo json).
        try
        {
            Assert.That(await Send("POSTICIPA", 1001, 2026, 7, Ente1, "SECONDO SALDO", "nota-1"), Is.EqualTo(1));

            var note = ReadNote(1001);
            TestContext.Out.WriteLine($"Note: {note}");
            Assert.That(note, Does.Contain("nota-1"));
        }
        finally { Cleanup(1001); }
    }

    // ---------- helper ----------

    private Task<int?> Send(string azione, int? idFattura, int anno, int mese, string ente, string tipologia, string testo = "itest") =>
        _handler.Send(new GestioneFattureAzioneCommand(AdminAuth())
        {
            IdEnte = ente,
            Azione = azione,
            Anno = anno,
            Mese = mese,
            TipologiaFattura = tipologia,
            IdUtente = "itest",
            IdFattura = idFattura,
            Nota = new NoteCommand { Data = DateTime.UtcNow, Testo = testo }
        });

    private int ReadStato(long idFattura) => Scalar<int>(
        "SELECT TOP(1) Stato FROM cfg.GestioneFatture WHERE FkIdFattura=@id ORDER BY Stato", ("@id", idFattura), -1);

    // Legge lo Stato per chiave di PERIODO (posticipa pre-generazione: la riga puo' avere FkIdFattura NULL).
    private int ReadStatoByPeriod(string ente, string tip, int anno, int mese)
    {
        using var conn = new SqlConnection(Conn); conn.Open();
        using var cmd = new SqlCommand(
            "SELECT TOP(1) Stato FROM cfg.GestioneFatture WHERE FkIdEnte=@e AND FkTipologiaFattura=@t AND Anno=@a AND Mese=@m", conn);
        cmd.Parameters.AddWithValue("@e", ente); cmd.Parameters.AddWithValue("@t", tip);
        cmd.Parameters.AddWithValue("@a", anno); cmd.Parameters.AddWithValue("@m", mese);
        var v = cmd.ExecuteScalar();
        return v is null or DBNull ? -1 : Convert.ToInt32(v);
    }

    private string ReadNote(long idFattura) => Scalar<string>(
        "SELECT TOP(1) CAST(Note AS nvarchar(max)) FROM cfg.GestioneFatture WHERE FkIdFattura=@id", ("@id", idFattura), "");

    private T Scalar<T>(string sql, (string, object) p, T fallback)
    {
        using var conn = new SqlConnection(Conn); conn.Open();
        using var cmd = new SqlCommand(sql, conn); cmd.Parameters.AddWithValue(p.Item1, p.Item2);
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
        catch (SqlException) { /* best-effort */ }
    }

    private void Cleanup(long idFattura) => Exec("DELETE FROM cfg.GestioneFatture WHERE FkIdFattura=@id", ("@id", idFattura));

    private void CleanupByPeriod(string ente, string tip, int anno, int mese) => Exec(
        "DELETE FROM cfg.GestioneFatture WHERE FkIdEnte=@e AND FkTipologiaFattura=@t AND Anno=@a AND Mese=@m",
        ("@e", ente), ("@t", tip), ("@a", anno), ("@m", mese));

    private record struct Fatt(long Id, string Ente, string Tip, int Anno, int Mese);

    private Fatt SnapshotFattura(long id)
    {
        using var conn = new SqlConnection(Conn); conn.Open();
        using var cmd = new SqlCommand(
            "SELECT IdFattura,FkIdEnte,FkTipologiaFattura,AnnoRiferimento,MeseRiferimento FROM pfd.FattureTestata WHERE IdFattura=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? new Fatt(r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetInt32(3), r.GetInt32(4)) : default;
    }

    private void RestoreEliminata(Fatt f)
    {
        if (f.Id == 0) return;
        Cleanup(f.Id);
        Exec("DELETE FROM pfd.FattureTestata_Eliminate WHERE IdFattura=@id", ("@id", f.Id));
        Exec(@"IF NOT EXISTS (SELECT 1 FROM pfd.FattureTestata WHERE IdFattura=@id)
               INSERT INTO pfd.FattureTestata(IdFattura,FkIdEnte,FkTipologiaFattura,AnnoRiferimento,MeseRiferimento,FatturaInviata)
               VALUES(@id,@e,@t,@a,@m,0)",
            ("@id", f.Id), ("@e", f.Ente), ("@t", f.Tip), ("@a", f.Anno), ("@m", f.Mese));
    }
}
