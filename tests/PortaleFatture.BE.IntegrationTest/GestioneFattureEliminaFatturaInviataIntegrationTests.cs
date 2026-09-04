using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Runbook **T09** — ELIMINA su una fattura **gia' inviata** (`FatturaInviata = 1`).
///
/// ⚠️ **DIFETTO NOTO, non un test rotto.** Misurato il 04/09/2026 eseguendo
/// `be.spGestioneFattureElimina` su una fattura ANTICIPO con `FatturaInviata = 1`:
///
/// <code>
/// Result                            = 1     ← l'API risponde OK
/// riga cfg.GestioneFatture          = 1, Stato = 3 (ELIMINATA)
/// fattura ancora in FattureTestata  = 1     ← NON spostata
/// spostata in _Eliminate            = 0
/// </code>
///
/// **Perche' succede.** La guardia sulle gia' inviate esiste
/// (`AND (ft.FatturaInviata IS NULL OR ft.FatturaInviata = 0)`) ma agisce **escludendo** la fattura
/// dalla tabella temporanea: il conteggio va a zero e si finisce nel ramo `ELSE`, che non rifiuta —
/// ricontrolla la sola whitelist di tipologia (ANTICIPO/ACCONTO, o PRIMO SALDO per INPS) e scrive
/// comunque la riga ELIMINATA. Il ramo `ELSE` non distingue *"fattura assente"* da *"fattura presente
/// ma gia' inviata"*, che sono due casi diversi.
///
/// **Conseguenza**: stato incoerente. Gestione Fatture mostra ELIMINATA, Documenti Emessi continua a
/// mostrare la fattura come emessa e inviata, e in "Non Fatturate" non compare (quel ramo della vista
/// legge `pfd.FattureTestata_Eliminate`, dove la fattura non e' finita). Dal 04/09/2026 c'e' un effetto
/// in piu': con l'esclusione `Stato IN (0,3)` aggiunta al report, quella fattura — realmente inviata —
/// sparisce anche dal report di saldo.
///
/// **Perche' resta [Ignore] e non viene corretto qui.** Il percorso **non e' raggiungibile dal
/// frontend**: le finestre temporali del form non offrono periodi con fatture gia' inviate. La SP resta
/// pero' invocabile direttamente dal team Data, quindi il caso e' reale e va lasciato documentato e
/// disponibile a loro. La SP ha un owner esterno: non si modifica da qui (v.
/// `docs/test-integrazione-db-seedato.md`, § convenzione test ↔ difetti SP).
///
/// **Attenzione a una frase da non ripetere al cliente**: *"in un caso reale non sarebbe stato
/// possibile procedere all'eliminazione"* e' vera solo a meta'. E' vero che la fattura **non viene
/// rimossa** dai documenti contabili; non e' vero che l'azione venga rifiutata.
///
/// Questo test esprime l'**aspettativa corretta** (rifiuto, `Result 0`, nessuna riga scritta): va
/// riattivato quando la SP distinguera' i due casi.
/// </summary>
[TestFixture]
public class GestioneFattureEliminaFatturaInviataIntegrationTests
{
    private const string Ente3 = "33333333-3333-3333-3333-333333333333";
    private const string Tipologia = "ANTICIPO";

    // Periodo remoto e riservato: nessun'altra fixture lo usa, e non collide con dati di scenario.
    private const int Anno = 2029;
    private const int Mese = 11;
    private const int IdFattura = 9901;

    private IMediator _handler = null!;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
        Pulisci();
        SeminaFatturaGiaInviata();
    }

    [TearDown]
    public void TearDown() => Pulisci();

    private static AuthenticationInfo AdminAuth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    private static void Esegui(string sql, params (string Nome, object Valore)[] parametri)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (nome, valore) in parametri)
            cmd.Parameters.AddWithValue(nome, valore);
        cmd.ExecuteNonQuery();
    }

    private static int Conta(string sql)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>Pulizia **per periodo**: dopo una ELIMINA `FkIdFattura` e' NULL e una delete per id non troverebbe la riga.</summary>
    private static void Pulisci()
    {
        Esegui("DELETE FROM cfg.GestioneFatture WHERE Anno = @a AND Mese = @m AND FkIdEnte = @e;",
            ("@a", Anno), ("@m", Mese), ("@e", Ente3));
        Esegui("DELETE FROM pfd.FattureTestata_Eliminate WHERE IdFattura = @id;", ("@id", IdFattura));
        Esegui("DELETE FROM pfd.FattureTestata WHERE IdFattura = @id;", ("@id", IdFattura));
    }

    private static void SeminaFatturaGiaInviata() => Esegui(@"
        SET IDENTITY_INSERT pfd.FattureTestata ON;
        INSERT INTO pfd.FattureTestata
            (IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, FatturaInviata,
             FkProdotto, FkIdTipoDocumento, DataFattura, IdentificativoFattura, TotaleFattura, Divisa,
             MetodoPagamento, Progressivo, CodiceContratto)
        VALUES (@id, @e, @t, @a, @m, 1, 'prod-pn', 'TD01', '2029-11-01', 'IT-9901', 100.00, 'EUR',
                'MP5', @id, 'TOKEN-E3');
        SET IDENTITY_INSERT pfd.FattureTestata OFF;",
        ("@id", IdFattura), ("@e", Ente3), ("@t", Tipologia), ("@a", Anno), ("@m", Mese));

    [Test]
    [Ignore("DIFETTO NOTO (misurato 04/09/2026): l'ELIMINA su una fattura gia' inviata non viene "
            + "rifiutata — torna Result 1 e scrive la riga ELIMINATA, pur non spostando la fattura. "
            + "Il ramo ELSE della SP non distingue 'fattura assente' da 'fattura presente ma inviata'. "
            + "Non raggiungibile dal frontend (le finestre del form non offrono quei periodi), ma la SP "
            + "resta invocabile dal team Data. Owner della SP: team DB. Riattivare quando corretta.")]
    public async Task Elimina_SuFatturaGiaInviata_DovrebbeEssereRifiutata()
    {
        var command = new GestioneFattureAzioneCommand(AdminAuth())
        {
            IdEnte = Ente3,
            Azione = "ELIMINA",
            Anno = Anno,
            Mese = Mese,
            TipologiaFattura = Tipologia,
            IdUtente = "itest-t09",
            IdFattura = null,
            Nota = new NoteCommand { Data = DateTime.Now, Testo = "T09: elimina su fattura gia' inviata" }
        };

        var result = await _handler.Send(command);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0),
                "Una fattura gia' inviata non deve poter essere eliminata: atteso un rifiuto esplicito.");

            Assert.That(Conta($"SELECT COUNT(*) FROM cfg.GestioneFatture WHERE Anno={Anno} AND Mese={Mese} AND FkIdEnte='{Ente3}'"),
                Is.EqualTo(0),
                "Nessuna riga di staging deve essere scritta per una fattura gia' inviata.");

            // Questa parte oggi passa gia': la fattura NON viene spostata. E' la meta' vera della
            // risposta data al cliente, e va mantenuta anche quando il resto sara' corretto.
            Assert.That(Conta($"SELECT COUNT(*) FROM pfd.FattureTestata WHERE IdFattura={IdFattura}"),
                Is.EqualTo(1), "La fattura gia' inviata non deve essere rimossa dai documenti contabili.");
            Assert.That(Conta($"SELECT COUNT(*) FROM pfd.FattureTestata_Eliminate WHERE IdFattura={IdFattura}"),
                Is.EqualTo(0), "La fattura gia' inviata non deve finire fra le eliminate.");
        });
    }
}
