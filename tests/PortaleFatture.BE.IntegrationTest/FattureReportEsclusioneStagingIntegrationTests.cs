using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Le fatture messe in staging (POSTICIPATE o ELIMINATE in `cfg.GestioneFatture`) non devono comparire
/// nel report delle **emesse**.
///
/// Perche' questi test esistono in questa forma. Il report non e' un foglio solo: `FattureRelExcelQuery`
/// restituisce piu' bucket, alimentati da **due query distinte** dello stesso builder — `_sqlRel` (foglio
/// REL) e `_sqlNoteSenzaRel` (foglio note/storni). La segnalazione nasceva dall'aver escluso la fattura
/// da una sola: spariva da un foglio e restava nell'altro, e il file risultava incoerente con se' stesso.
///
/// Per questo le asserzioni sono su **tutti i bucket appiattiti** e non su un indice: cio' che conta e'
/// "non compare da nessuna parte", che e' anche il requisito di business. Asserire sul bucket giusto
/// renderebbe il test verde anche se la riga ricomparisse nell'altro foglio.
///
/// Seed usato (v. `tests/Data/gestione_fatture.sql`), periodo **2026 / mese 2 / SECONDO SALDO**:
/// - fattura **8001**, ente1 — ha una RelTestata corrispondente, quindi finisce nel foglio **REL**;
/// - fattura **8101**, ente8 — riga `STORNO ANT. NA` e **nessuna** RelTestata, quindi finisce nel
///   foglio **note**. Aggiunta il 04/09/2026: prima quel foglio era vuoto nel seed (misurato: 0 righe),
///   e nessun test avrebbe potuto accorgersi della meta' mancante della fix.
///
/// I due enti sono distinti apposta: ogni test posticipa **un solo** periodo e verifica che l'altra
/// fattura resti — cosi' un'esclusione troppo larga (che svuotasse il report) verrebbe intercettata.
/// </summary>
public class FattureReportEsclusioneStagingIntegrationTests
{
    private const int Anno = 2026;
    private const int Mese = 2;
    private const string Tipologia = "SECONDO SALDO";

    private const string Ente1 = "11111111-1111-1111-1111-111111111111";
    private const string Ente8 = "88888888-8888-8888-8888-888888888888";

    private const string FatturaFoglioRel = "8001";
    private const string FatturaFoglioNote = "8101";

    /// <summary>
    /// Le **quattro tipologie di saldo** che il runbook T20 chiede di coprire, ciascuna con la coppia
    /// di fatture che esercita le due meta' dello sheet "Enti Fatt.": una con RelTestata (ramo
    /// `_sqlRel`) e una con riga STORNO e senza Rel (ramo `_sqlNoteSenzaRel`).
    /// Seed in `tests/Data/gestione_fatture.sql`.
    /// </summary>
    private static readonly object[] TipologieDiSaldo =
    [
        new object[] { "SECONDO SALDO",   "8001", "8101" },
        new object[] { "PRIMO SALDO",     "8201", "8202" },
        new object[] { "VAR. SEMESTRALE", "8301", "8302" },
        new object[] { "SEM. SOSPESI",    "8401", "8402" },
    ];

    private static readonly string[] TutteLeTipologie =
        ["SECONDO SALDO", "PRIMO SALDO", "VAR. SEMESTRALE", "SEM. SOSPESI"];

    private IMediator _handler = null!;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
        Pulisci();
    }

    [TearDown]
    public void TearDown() => Pulisci();

    /// <summary>
    /// Pulizia **per periodo** e non per FkIdFattura: dopo una ELIMINA quella colonna e' NULL, quindi
    /// una cancellazione per id lascerebbe la riga a occupare la chiave per sempre.
    /// </summary>
    private static void Pulisci()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        // Pulisce TUTTE le tipologie coperte, non solo quella del test in corso: un run interrotto a
        // meta' lascerebbe altrimenti una riga a occupare la chiave e i test successivi fallirebbero
        // per un motivo che non ha nulla a che vedere con cio' che verificano.
        cmd.CommandText = @"
            DELETE FROM cfg.GestioneFatture
            WHERE Anno = @anno AND Mese = @mese
              AND FkIdEnte IN (@ente1, @ente8);";
        cmd.Parameters.AddWithValue("@anno", Anno);
        cmd.Parameters.AddWithValue("@mese", Mese);
        cmd.Parameters.AddWithValue("@ente1", Ente1);
        cmd.Parameters.AddWithValue("@ente8", Ente8);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Scrive direttamente la riga di staging invece di passare dalle stored procedure: qui interessa
    /// l'effetto sul report a fronte di un certo <paramref name="stato"/>, non il percorso che lo ha
    /// prodotto — quello e' gia' coperto dai test di GestioneFatture.
    /// </summary>
    private static void MettiInStaging(string idEnte, int stato, string? tipologia = null)
    {
        tipologia ??= Tipologia;
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO cfg.GestioneFatture
                (FkIdEnte, FkTipologiaFattura, Anno, Mese, Stato, Azione, IdUtenteInserimento, DataInserimento)
            VALUES (@ente, @tipologia, @anno, @mese, @stato, @azione, 'integration-test-user', GETDATE());";
        cmd.Parameters.AddWithValue("@ente", idEnte);
        cmd.Parameters.AddWithValue("@tipologia", tipologia);
        cmd.Parameters.AddWithValue("@anno", Anno);
        cmd.Parameters.AddWithValue("@mese", Mese);
        cmd.Parameters.AddWithValue("@stato", stato);
        cmd.Parameters.AddWithValue("@azione", stato switch
        {
            0 => "POSTICIPATA",
            1 => "RIPRISTINATA",
            2 => "CANCELLATA",
            _ => "ELIMINATA"
        });
        cmd.ExecuteNonQuery();
    }

    private static AuthenticationInfo AdminAuth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    /// <summary>Tutti i fogli appiattiti: il requisito e' "non compare da nessuna parte".</summary>
    private async Task<List<IEnumerable<FattureRelExcelDto>>> BucketDelReport(string? tipologia = null)
    {
        var result = await _handler.Send(new FattureRelExcelQuery(AdminAuth())
        {
            Anno = Anno,
            Mese = Mese,
            TipologiaFattura = tipologia ?? Tipologia
        });

        Assert.That(result, Is.Not.Null, "La query del report non ha restituito bucket.");
        return result!;
    }

    private async Task<List<FattureRelExcelDto>> TutteLeRigheDelReport(string? tipologia = null) =>
        (await BucketDelReport(tipologia)).SelectMany(bucket => bucket).ToList();

    private static IEnumerable<string?> Fatture(List<FattureRelExcelDto> righe) => righe.Select(r => r.IdFattura);

    [Test]
    public async Task SenzaStaging_EntrambiIFogliContengonoLaLoroFattura()
    {
        var righe = await TutteLeRigheDelReport();

        // Controllo di base che rende significativi tutti gli altri: se il seed non producesse queste
        // due righe, i test sull'esclusione passerebbero per assenza di dati invece che per la fix.
        Assert.Multiple(() =>
        {
            Assert.That(Fatture(righe), Does.Contain(FatturaFoglioRel),
                "Manca la fattura del foglio REL: seed non applicato?");
            Assert.That(Fatture(righe), Does.Contain(FatturaFoglioNote),
                "Manca la fattura del foglio note/storni: v. il blocco 8101 in gestione_fatture.sql.");
        });
    }

    [Test]
    public async Task Posticipata_SparisceDalFoglioRel_ELAltraResta()
    {
        MettiInStaging(Ente1, stato: 0);

        var righe = await TutteLeRigheDelReport();

        Assert.Multiple(() =>
        {
            Assert.That(Fatture(righe), Does.Not.Contain(FatturaFoglioRel),
                "La fattura posticipata compare ancora nel report.");
            Assert.That(Fatture(righe), Does.Contain(FatturaFoglioNote),
                "L'esclusione ha rimosso anche una fattura che non era in staging: troppo larga.");
        });
    }

    /// <summary>
    /// Il caso della segnalazione. Prima della fix questa riga restava nel foglio note anche dopo la
    /// posticipa, perche' l'esclusione era stata aggiunta solo a `_sqlRel`.
    /// </summary>
    [Test]
    public async Task Posticipata_SparisceAncheDalFoglioNoteStorni()
    {
        MettiInStaging(Ente8, stato: 0);

        var righe = await TutteLeRigheDelReport();

        Assert.Multiple(() =>
        {
            Assert.That(Fatture(righe), Does.Not.Contain(FatturaFoglioNote),
                "La fattura posticipata compare ancora nel foglio note/storni: l'esclusione e' stata "
                + "applicata solo a _sqlRel e il file resta incoerente con se' stesso.");
            Assert.That(Fatture(righe), Does.Contain(FatturaFoglioRel));
        });
    }

    [TestCase(0)]
    [TestCase(3)]
    public async Task StatiEsclusi_NonCompaionoInNessunFoglio(int stato)
    {
        MettiInStaging(Ente1, stato);
        MettiInStaging(Ente8, stato);

        var righe = await TutteLeRigheDelReport();

        Assert.Multiple(() =>
        {
            Assert.That(Fatture(righe), Does.Not.Contain(FatturaFoglioRel));
            Assert.That(Fatture(righe), Does.Not.Contain(FatturaFoglioNote));
        });
    }

    /// <summary>
    /// Il rovescio, altrettanto importante: RIPRISTINATA (1) e CANCELLATA (2) sono periodi **tornati in
    /// gioco**, e devono restare nel report. Un'esclusione scritta come "esiste una riga in staging"
    /// invece che "esiste una riga con Stato IN (0,3)" li perderebbe — ed e' esattamente la differenza
    /// fra i due predicati gia' in uso altrove nel codebase.
    /// </summary>
    /// <summary>
    /// Il caso dei runbook **T19 e T20**, segnalati KO: *"nello sheet Enti Fatt. MESE non devono essere
    /// visualizzate le fatture Posticipate e/o Eliminate presenti nello sheet Non Fatturate"*.
    ///
    /// Perche' serve un test dedicato quando gli altri gia' appiattiscono tutti i bucket: qui si ancora
    /// la **corrispondenza sheet ↔ bucket**, che altrimenti resta implicita e si perde alla prima
    /// modifica di `ReportFattureRel`. La catena, verificata il 04/09/2026:
    ///
    ///   `FattureRelExcelHandler` ritorna `{ rel, relsu, relno }`
    ///        indice 0 → sheet "Regolari Esecuzioni {mese}"
    ///        indice 1 → sheet **"Enti Fatt. {mese}"**   ← quello della segnalazione
    ///        indice 2 → (non reso come sheet a se')
    ///
    /// L'indice 1 e' `FattureUnionRelExcelPersistence`, cioe' `SelectRel() UNION SelectNoteSenzaRel()`:
    /// **entrambe** le query devono escludere lo staging, ed e' il motivo per cui correggerne una sola
    /// lasciava il difetto in piedi.
    /// </summary>
    [TestCaseSource(nameof(TipologieDiSaldo))]
    public async Task ShEntiFatt_NonMostraLePosticipate(string tipologia, string fatturaRel, string fatturaNote)
    {
        MettiInStaging(Ente1, stato: 0, tipologia);
        MettiInStaging(Ente8, stato: 0, tipologia);

        var bucket = await BucketDelReport(tipologia);
        Assert.That(bucket.Count, Is.GreaterThan(1), "Manca il bucket che alimenta lo sheet Enti Fatt.");
        var sheetEntiFatt = bucket[1].Select(r => r.IdFattura).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(sheetEntiFatt, Does.Not.Contain(fatturaRel),
                $"T19/T20 ({tipologia}): una fattura posticipata compare nello sheet Enti Fatt.");
            Assert.That(sheetEntiFatt, Does.Not.Contain(fatturaNote),
                $"T19/T20 ({tipologia}): la meta' UNION (note/storni) dello sheet non filtra lo staging.");
        });
    }

    [TestCaseSource(nameof(TipologieDiSaldo))]
    public async Task ShEntiFatt_NonMostraLeEliminate(string tipologia, string fatturaRel, string fatturaNote)
    {
        MettiInStaging(Ente1, stato: 3, tipologia);
        MettiInStaging(Ente8, stato: 3, tipologia);

        var bucket = await BucketDelReport(tipologia);
        var sheetEntiFatt = bucket[1].Select(r => r.IdFattura).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(sheetEntiFatt, Does.Not.Contain(fatturaRel),
                $"T19/T20 ({tipologia}): una fattura eliminata compare nello sheet Enti Fatt.");
            Assert.That(sheetEntiFatt, Does.Not.Contain(fatturaNote),
                $"T19/T20 ({tipologia}): la meta' UNION (note/storni) dello sheet non filtra lo staging.");
        });
    }

    /// <summary>
    /// Controllo di base per ciascuna tipologia: senza staging **entrambe** le fatture devono esserci.
    /// Senza questo, i due test sopra passerebbero anche se il seed di una tipologia sparisse — per
    /// assenza di dati invece che per effetto della fix.
    /// </summary>
    [TestCaseSource(nameof(TipologieDiSaldo))]
    public async Task SenzaStaging_OgniTipologiaHaEntrambeLeFatture(string tipologia, string fatturaRel, string fatturaNote)
    {
        var righe = await TutteLeRigheDelReport(tipologia);

        Assert.Multiple(() =>
        {
            Assert.That(Fatture(righe), Does.Contain(fatturaRel), $"Seed mancante per {tipologia} (ramo REL).");
            Assert.That(Fatture(righe), Does.Contain(fatturaNote), $"Seed mancante per {tipologia} (ramo note).");
        });
    }

    [TestCase(1)]
    [TestCase(2)]
    public async Task StatiTornatiInGioco_RestanoNelReport(int stato)
    {
        MettiInStaging(Ente1, stato);
        MettiInStaging(Ente8, stato);

        var righe = await TutteLeRigheDelReport();

        Assert.Multiple(() =>
        {
            Assert.That(Fatture(righe), Does.Contain(FatturaFoglioRel));
            Assert.That(Fatture(righe), Does.Contain(FatturaFoglioNote));
        });
    }
}
