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
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString); // container spento -> ignora, non fallisce
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    private string Conn => LocalTestDb.ConnectionString;

    private static AuthenticationInfo AdminAuth() => new()
    { IdEnte = Guid.NewGuid().ToString(), Prodotto = "prod-pn", Ruolo = Ruolo.ADMIN, IdTipoContratto = 1 };

    // ---------- 1) VINCOLI TIPOLOGIA x AZIONE ----------

    [TestCase(2001, "ANTICIPO", Ente3)]  // ANTICIPO non posticipabile
    [TestCase(2002, "ACCONTO", Ente3)]   // ACCONTO non posticipabile
    [Ignore("REQUISITO DA CHIARIRE (non basta il fix del bug): questi test erano VERDI con la SP "
          + "precedente. La spGestioneFatturePosticipa del 2026-07-24 abilita la pre-generazione e con "
          + "essa rifiuta UNA SOLA cosa: la fattura gia' inviata. La tipologia compare solo dentro quella "
          + "query di controllo, quindi una POSTICIPA su ANTICIPO/ACCONTO non trova nulla e finisce nel "
          + "ramo ELSE che inserisce. Attenzione: rimuovere la riga che azzera @countFatture (vedi "
          + "Posticipa_OnAlreadySentInvoice_ShouldBeRejected) NON fa ripassare questi test, perche' il "
          + "vincolo di tipologia non e' piu' espresso da nessuna parte. Domanda per PO/owner SP: la "
          + "posticipa deve restare limitata alle tipologie di saldo? Se si', il controllo va aggiunto "
          + "(in SP o come validazione dell'endpoint, dove gia' vive la whitelist delle azioni).")]
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
    [Ignore("BUG be.spGestioneFatturePosticipa (versione 2026-07-24): la guardia 'fattura gia' inviata' e' "
          + "codice morto. La SP calcola @countFatture su pfd.FattureTestata e SUBITO DOPO lo sovrascrive "
          + "con 'SELECT @countFatture = COUNT(*) FROM @tmpGestioneFatture'; quella tabella variabile viene "
          + "solo DELETE-ata e mai popolata, quindi @countFatture vale sempre 0 e il ramo di rifiuto non "
          + "viene mai preso. Verificato sul container: con FatturaInviata=1 la SP torna Result 1 e crea la "
          + "riga. Fix: eliminare la riga che sovrascrive, nient'altro: la condizione @countFatture = 1 "
          + "resta corretta perche' per una data chiave (ente, tipologia, anno, mese) esiste una sola "
          + "fattura, quindi il conteggio vale 0 oppure 1.")]
    public async Task Posticipa_OnAlreadySentInvoice_ShouldBeRejected()
    {
        // La SP dichiara di voler rifiutare la posticipa di una fattura gia' inviata (cerca in
        // pfd.FattureTestata le righe con FatturaInviata = 1). Questo test esercita quella intenzione.
        try
        {
            Exec("UPDATE pfd.FattureTestata SET FatturaInviata = 1 WHERE IdFattura = 1001");

            var r = await Send("POSTICIPA", 1001, 2026, 7, Ente1, "SECONDO SALDO");
            Assert.That(r, Is.EqualTo(0), "POSTICIPA su fattura gia' INVIATA doveva essere rifiutata (Result 0).");
            Assert.That(ReadStato(1001), Is.EqualTo(-1), "Non deve essere creata alcuna riga in cfg.GestioneFatture.");
        }
        finally
        {
            Exec("UPDATE pfd.FattureTestata SET FatturaInviata = 0 WHERE IdFattura = 1001");
            Cleanup(1001);
        }
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

    // ---------- 3) REQUISITO: posticipa PRE-GENERAZIONE ----------

    [Test]
    public async Task Posticipa_PreGeneration_NoInvoiceYet_ShouldSucceed_ByPeriod()
    {
        // Requisito AGGIORNATO: l'admin puo' posticipare un periodo (Ente/Tipologia/Anno/Mese) anche se la
        // fattura non esiste ancora in FattureTestata (PAC in attesa firma REL). Deve creare la riga
        // cfg.GestioneFatture con Stato=0 (POSTICIPATA) e FkIdFattura NULL.
        // Sbloccato dalla SP spGestioneFatturePosticipa del 2026-07-24, che non pretende piu' la fattura
        // esistente: cerca solo una fattura GIA' INVIATA da rifiutare, e in assenza inserisce.
        try
        {
            var r = await Send("POSTICIPA", idFattura: null, anno: 1999, mese: 1, ente: Ente1, tipologia: "SECONDO SALDO");
            Assert.That(r, Is.EqualTo(1), "Posticipa pre-generazione deve riuscire (Result 1).");
            Assert.That(ReadStatoByPeriod(Ente1, "SECONDO SALDO", 1999, 1), Is.EqualTo(0),
                "Deve essere creata la riga POSTICIPATA (Stato=0) per periodo, senza fattura esistente.");
        }
        finally { CleanupByPeriod(Ente1, "SECONDO SALDO", 1999, 1); }
    }

    // ---------- 3-ter) MACCHINA A STATI a livello MediatR (handler -> SP) ----------
    //
    // Stessi 4 scenari coperti via HTTP in Http/GestioneFattureHttpTests, ma qui il contratto e' il
    // valore di Result restituito dalla SP (0 = rifiutata, 1 = eseguita), non lo status code.
    // Tenerli su entrambi i livelli non e' ridondante: separa "la SP applica la regola" da "l'endpoint
    // la traduce bene per il client". Oggi il primo e' corretto e il secondo no (Result 0 -> 404).
    // Chiave per PERIODO (senza IdFattura), come le chiamate reali del frontend.

    private const int AnnoSm = 2026;
    private const int MeseSm = 9;
    private const string TipSm = "SECONDO SALDO";

    [Test]
    public async Task Sm_PosticipaSuGiaPosticipata_ShouldReturn0()
    {
        try
        {
            Assert.That(await Send("POSTICIPA", null, AnnoSm, MeseSm, Ente1, TipSm), Is.EqualTo(1));
            Assert.That(await Send("POSTICIPA", null, AnnoSm, MeseSm, Ente1, TipSm), Is.EqualTo(0),
                "Posticipare una gia' POSTICIPATA va rifiutato (Result 0).");
            Assert.That(ContaRighe(Ente1, TipSm, AnnoSm, MeseSm), Is.EqualTo(1),
                "e non deve lasciare righe in piu'.");
        }
        finally { CleanupByPeriod(Ente1, TipSm, AnnoSm, MeseSm); }
    }

    [Test]
    public async Task Sm_RipristinaSuGiaRipristinata_ShouldReturn0()
    {
        try
        {
            Assert.That(await Send("POSTICIPA", null, AnnoSm, MeseSm, Ente1, TipSm), Is.EqualTo(1));
            Assert.That(await Send("RIPRISTINA", null, AnnoSm, MeseSm, Ente1, TipSm), Is.EqualTo(1));
            Assert.That(await Send("RIPRISTINA", null, AnnoSm, MeseSm, Ente1, TipSm), Is.EqualTo(0),
                "Ripristinare una gia' RIPRISTINATA va rifiutato (Result 0).");
        }
        finally { CleanupByPeriod(Ente1, TipSm, AnnoSm, MeseSm); }
    }

    [Test]
    public async Task Sm_RipristinaSuPosticipata_ShouldReturn1()
    {
        try
        {
            Assert.That(await Send("POSTICIPA", null, AnnoSm, MeseSm, Ente1, TipSm), Is.EqualTo(1));
            Assert.That(await Send("RIPRISTINA", null, AnnoSm, MeseSm, Ente1, TipSm), Is.EqualTo(1),
                "Una POSTICIPATA deve poter essere ripristinata.");
            Assert.That(ContaRighe(Ente1, TipSm, AnnoSm, MeseSm), Is.EqualTo(1),
                "la transizione aggiorna la riga esistente, non ne aggiunge una.");
        }
        finally { CleanupByPeriod(Ente1, TipSm, AnnoSm, MeseSm); }
    }

    [Test]
    [Ignore("DIFETTO CONFERMATO, stesso di PosticipaRipristina_Ripetuto_ShouldKeepOneRowPerPeriod: la "
          + "transizione RIPRISTINATA -> POSTICIPATA riesce (Result 1) ma INSERISCE una seconda riga per "
          + "lo stesso periodo invece di riportare a Stato 0 quella esistente. Da li' in poi il periodo "
          + "ha due righe e il ripristino successivo viola la PK. Rimedio: Fix 8 in "
          + "segnalazione-sp-gestione-fatture.md.")]
    public async Task Sm_PosticipaSuRipristinata_ShouldReturn1_AndKeepOneRow()
    {
        try
        {
            Assert.That(await Send("POSTICIPA", null, AnnoSm, MeseSm, Ente1, TipSm), Is.EqualTo(1));
            Assert.That(await Send("RIPRISTINA", null, AnnoSm, MeseSm, Ente1, TipSm), Is.EqualTo(1));
            Assert.That(await Send("POSTICIPA", null, AnnoSm, MeseSm, Ente1, TipSm), Is.EqualTo(1),
                "Una RIPRISTINATA deve poter essere posticipata di nuovo.");
            Assert.That(ContaRighe(Ente1, TipSm, AnnoSm, MeseSm), Is.EqualTo(1),
                "e deve restare UNA riga per periodo.");
        }
        finally { CleanupByPeriod(Ente1, TipSm, AnnoSm, MeseSm); }
    }

    // ---------- 3-bis) CICLO POSTICIPA / RIPRISTINA RIPETUTO ----------

    [Test]
    [Ignore("DIFETTO CONFERMATO (riproducibile da UI con click normali). Alternando POSTICIPA e "
          + "RIPRISTINA sullo stesso periodo: il 1o giro va (1,1), la 2a POSTICIPA torna 1 ma crea una "
          + "SECONDA riga per lo stesso periodo (Stato 0 accanto a Stato 1), e la 2a RIPRISTINA fallisce "
          + "con 'Violation of PRIMARY KEY constraint PK_GestioneFatture ... (ente, tipologia, anno, mese, 1)'. "
          + "Causa: Stato fa parte della PK, RIPRISTINA fa UPDATE dello Stato in place e il suo MERGE "
          + "matcha su (Anno, Mese, Tipologia, Ente) SENZA Stato, quindi non distingue le righe. "
          + "Lo stato intermedio a due righe e' gia' incoerente di suo: la griglia ne mostrerebbe due per "
          + "lo stesso periodo. Rimedio in segnalazione-sp-gestione-fatture.md (Fix 8).")]
    public async Task PosticipaRipristina_Ripetuto_ShouldKeepOneRowPerPeriod()
    {
        try
        {
            Assert.That(await Send("POSTICIPA", null, 2026, 8, Ente1, "PRIMO SALDO"), Is.EqualTo(1), "1a posticipa");
            Assert.That(await Send("RIPRISTINA", null, 2026, 8, Ente1, "PRIMO SALDO"), Is.EqualTo(1), "1o ripristino");
            Assert.That(await Send("POSTICIPA", null, 2026, 8, Ente1, "PRIMO SALDO"), Is.EqualTo(1), "2a posticipa");

            Assert.That(ContaRighe(Ente1, "PRIMO SALDO", 2026, 8), Is.EqualTo(1),
                "Per un periodo deve esistere UNA riga: la seconda posticipa non deve affiancarne una nuova.");

            Assert.That(await Send("RIPRISTINA", null, 2026, 8, Ente1, "PRIMO SALDO"), Is.EqualTo(1),
                "2o ripristino: oggi fallisce con violazione di PK.");
        }
        finally { CleanupByPeriod(Ente1, "PRIMO SALDO", 2026, 8); }
    }

    private int ContaRighe(string ente, string tip, int anno, int mese)
    {
        using var conn = new SqlConnection(Conn); conn.Open();
        using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM cfg.GestioneFatture WHERE FkIdEnte=@e AND FkTipologiaFattura=@t AND Anno=@a AND Mese=@m", conn);
        cmd.Parameters.AddWithValue("@e", ente); cmd.Parameters.AddWithValue("@t", tip);
        cmd.Parameters.AddWithValue("@a", anno); cmd.Parameters.AddWithValue("@m", mese);
        return Convert.ToInt32(cmd.ExecuteScalar());
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

    // ---------- 3-quater) CANCELLA rifiutata: RF06 (già calcolata) e mismatch IdFattura ----------
    //
    // Casi diagnosticati da una segnalazione reale (CANCELLA su ANTICIPO che "dava errore"). La SP non
    // lancia: restituisce Result 0. L'endpoint oggi lo traduce in 404 muto (finding #9). Questi test
    // fissano i due modi legittimi/insidiosi in cui la CANCELLA torna 0, cosi' che:
    //   - la correzione SP in corso (colonna @err_message nel result set) abbia un criterio di verifica;
    //   - la fix endpoint (Result 0 -> 400 con messaggio) sia testabile.

    [Test]
    public async Task Cancella_QuandoFatturaGiaInEliminate_ShouldReturn0_PerRF06()
    {
        // RF06: una pre-eliminata e' cancellabile SOLO finche' non e' calcolata lato processo DATA.
        // Se la fattura del periodo e' gia' in pfd.FattureTestata_Eliminate, la SP rifiuta (Result 0).
        const int anno = 2026, mese = 2; const string tip = "ANTICIPO";
        try
        {
            InsertInEliminate(Ente3, tip, anno, mese, idFattura: 62865);
            var r = await Send("CANCELLA", idFattura: null, anno, mese, Ente3, tip);
            Assert.That(r, Is.EqualTo(0),
                "Fattura gia' in FattureTestata_Eliminate (calcolata lato DATA): CANCELLA rifiutata (RF06).");
        }
        finally { CleanupInEliminate(Ente3, tip, anno, mese); CleanupByPeriod(Ente3, tip, anno, mese); }
    }

    [Test]
    public async Task Cancella_PerPeriodo_SuStagingConFkIdFatturaNull_ShouldReturn1()
    {
        // La chiave del sistema e' il PERIODO (Q&A): un ANTICIPO messo in staging per periodo ha
        // FkIdFattura NULL. La CANCELLA per periodo (senza IdFattura) lo trova e riesce.
        const int anno = 2026, mese = 3; const string tip = "ANTICIPO";
        try
        {
            InsertStaging(Ente3, tip, anno, mese, stato: 3, azione: "ELIMINATA", fkIdFattura: null);
            var r = await Send("CANCELLA", idFattura: null, anno, mese, Ente3, tip);
            Assert.That(r, Is.EqualTo(1), "CANCELLA per periodo su record staging valido: Result 1.");
        }
        finally { CleanupByPeriod(Ente3, tip, anno, mese); }
    }

    [Test]
    public async Task Cancella_ConIdFattura_SuStagingConFkIdFatturaNull_ShouldReturn0_Caratterizzazione()
    {
        // CARATTERIZZAZIONE del difetto di contratto: se il chiamante passa un IdFattura ma il record
        // staging ha FkIdFattura NULL (creato per periodo), il filtro 'AND gf.FkIdFattura = @IdFattura'
        // esclude il record -> Result 0. Il filtro contraddice la chiave-di-periodo dei requisiti.
        // Se un domani la SP togliera'/tollerera' quel filtro, questo test diventera' rosso: e' il segnale.
        const int anno = 2026, mese = 4; const string tip = "ANTICIPO";
        try
        {
            InsertStaging(Ente3, tip, anno, mese, stato: 3, azione: "ELIMINATA", fkIdFattura: null);
            var r = await Send("CANCELLA", idFattura: 62899, anno, mese, Ente3, tip);
            Assert.That(r, Is.EqualTo(0),
                "Comportamento attuale: IdFattura valorizzato + record per-periodo (FkIdFattura NULL) -> no match -> 0.");
        }
        finally { CleanupByPeriod(Ente3, tip, anno, mese); }
    }

    private void InsertStaging(string ente, string tip, int anno, int mese, int stato, string azione, int? fkIdFattura) => Exec(@"
        INSERT INTO cfg.GestioneFatture
            (FkIdEnte, FkTipologiaFattura, Anno, Mese, DataInserimento, IdUtenteInserimento, Stato, Azione, FkIdFattura, Note)
        VALUES (@e, @t, @a, @m, GETDATE(), 'itest', @s, @az, @f, N'{""Data"":""2026-01-01T00:00:00"",""Testo"":""seed""}')",
        ("@e", ente), ("@t", tip), ("@a", anno), ("@m", mese), ("@s", stato), ("@az", azione),
        ("@f", (object?)fkIdFattura ?? DBNull.Value));

    private void InsertInEliminate(string ente, string tip, int anno, int mese, int idFattura) => Exec(@"
        INSERT INTO pfd.FattureTestata_Eliminate
            (IdFattura, FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura,
             IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento, MeseRiferimento, FlagProceduraWhiteList)
        VALUES (@id, 'prod-pn', 'TD01', @t, @e, '2026-01-01', CONCAT('IT-', @id), 100, 'EUR', 'MP5', @a, @m, 0)",
        ("@id", idFattura), ("@t", tip), ("@e", ente), ("@a", anno), ("@m", mese));

    private void CleanupInEliminate(string ente, string tip, int anno, int mese) => Exec(
        "DELETE FROM pfd.FattureTestata_Eliminate WHERE FkIdEnte=@e AND FkTipologiaFattura=@t AND AnnoRiferimento=@a AND MeseRiferimento=@m",
        ("@e", ente), ("@t", tip), ("@a", anno), ("@m", mese));

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
        // IdFattura e' IDENTITY (DDL reale): serve IDENTITY_INSERT per rimettere lo stesso id, e vanno
        // valorizzate le colonne NOT NULL della tabella vera.
        Exec(@"IF NOT EXISTS (SELECT 1 FROM pfd.FattureTestata WHERE IdFattura=@id)
               BEGIN
                 SET IDENTITY_INSERT pfd.FattureTestata ON;
                 INSERT INTO pfd.FattureTestata
                    (IdFattura,FkIdEnte,FkTipologiaFattura,AnnoRiferimento,MeseRiferimento,FatturaInviata,
                     FkProdotto,FkIdTipoDocumento,DataFattura,IdentificativoFattura,TotaleFattura,Divisa,MetodoPagamento,Progressivo)
                 VALUES(@id,@e,@t,@a,@m,0,
                     'prod-pn','TD01','2026-01-01',CONCAT('IT-',@id),100.00,'EUR','MP5',@id);
                 SET IDENTITY_INSERT pfd.FattureTestata OFF;
               END",
            ("@id", f.Id), ("@e", f.Ente), ("@t", f.Tip), ("@a", f.Anno), ("@m", f.Mese));
    }
}
