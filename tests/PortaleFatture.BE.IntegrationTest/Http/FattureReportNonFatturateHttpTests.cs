using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Il caso di testbook `PF-672 TD-18`: il **Download Report** di Documenti Emessi deve contenere lo
/// sheet **"Non Fatturate"** con le fatture Posticipate ed Eliminate, e deve esserci **in tutti** i
/// report di saldo: `PRIMO SALDO`, `SECONDO SALDO`, `VAR. SEMESTRALE`, `SEM. SOSPESI`.
///
/// La presenza dello sheet è già coperta a **unit** su `ReportFattureRel` — fu un difetto reale: lo
/// sheet veniva aggiunto solo nel ramo `else` del loop sui gruppi, quindi compariva solo con tre o più
/// gruppi e spariva pur avendo dati (corretto il 29/07/2026). Qui si verifica l'altra metà, quella che
/// l'utente vede: che **nel file scaricato** lo sheet ci sia davvero.
///
/// Il report si scarica come **zip** contenente uno o più `.xlsx`; i nomi dei fogli stanno in
/// `xl/workbook.xml` di ciascuno. Si apre quindi lo zip, poi gli xlsx dentro — stessa tecnica di
/// TD-11, un livello più in profondità.
///
/// Le righe di staging necessarie sono create **dal test** e ripulite: quelle statiche del 2025 non si
/// possono usare perché altre classi ne asseriscono il numero esatto, e aggiungerne le romperebbe.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class FattureReportNonFatturateHttpTests
{
    private const string Rotta = "/api/fatture/report";
    private const string RottaNonInviate = "/api/fatture/pagopa/non-inviate/report";

    // Periodo del seed che ha davvero fatture da riportare (fattura 8001, SECONDO SALDO).
    private const int Anno = 2026;
    private const int Mese = 2;

    // Le righe di staging vivono su un periodo tutto loro: la vista del report non filtra per anno
    // (e' un report globale), quindi basta che esistano.
    private const string IdEnte = "11111111-1111-1111-1111-111111111111";
    private const int AnnoStaging = 2028;

    // La fattura NON inviata usata per il report della pagina Invia Fatture (TD-20).
    private const long IdFatturaNonInviata = 7501;
    private const string EnteNonInviata = "77777777-7777-7777-7777-777777777777";
    private const string TipologiaNonInviata = "VAR. SEMESTRALE";

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void OneTimeSetup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Pulisci();
        _factory?.Dispose();
    }

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        Pulisci();
    }

    [TearDown]
    public void TearDown() => Pulisci();

    // =============================================================================================

    [Test]
    public async Task Report_ConPosticipate_ShouldContenereLoSheetNonFatturate()
    {
        SeminaStaging("SECONDO SALDO", 2);

        var (stato, fogli, _, _) = await Report("SECONDO SALDO");

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(fogli, Does.Contain("Non Fatturate"),
            "E' il difetto corretto il 29/07/2026 — lo sheet veniva aggiunto solo nel ramo else del "
            + "loop sui gruppi, quindi compariva solo con >= 3 gruppi. Se sparisce, e' una regressione.");
    }

    /// <summary>
    /// CARATTERIZZAZIONE — **lo sheet è per tipologia, non globale**, e questa è la scoperta di TD-18.
    ///
    /// Il testbook dice *"nel quale compaiono tutte le fatture Posticipate ed Eliminate presenti nella
    /// pagina Gestione Fatture"*; il codice invece filtra:
    /// `gestioneFattureReport?.Where(x =&gt; x.TipologiaFattura == tipologia)` — quindi nel report del
    /// `SECONDO SALDO` compaiono solo le non fatturate di `SECONDO SALDO`, non quelle delle altre
    /// tipologie.
    ///
    /// Le due letture sono entrambe difendibili: "tutte" può voler dire *tutte quelle di quella
    /// tipologia* (e un report di primo saldo che elencasse anticipi sarebbe strano), oppure il
    /// requisito chiede davvero un elenco unico. **Non è una decisione da prendere qui**: il test fissa
    /// il comportamento reale, e la lettura letterale del testbook resta in un `[Ignore]` accanto.
    ///
    /// Conseguenza pratica, quella che conta per chi collauda: una posticipata su una tipologia che per
    /// quel periodo **non ha fatture** non compare in nessun report, perché il report di quella
    /// tipologia non viene proprio generato.
    /// </summary>
    [Test]
    public async Task Report_LoSheet_ShouldContenereSoloLaTipologiaDelReport_Caratterizzazione()
    {
        SeminaStaging("PRIMO SALDO", 1);
        SeminaStaging("VAR. SEMESTRALE", 3, stato: 3, azione: "ELIMINATA");

        // Un solo report, e confronto sul SOLO foglio "Non Fatturate". Il testo dell'intero workbook
        // non discrimina piu' nulla: da quando il seed ha fatture 2026/2 per tutte e quattro le
        // tipologie, chiedendole tutte lo zip contiene un xlsx per tipologia e i fogli di dettaglio di
        // ciascuno citano legittimamente la propria.
        var (stato, fogli, _, perFoglio) = await Report("SECONDO SALDO");

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(fogli, Does.Contain("Non Fatturate"));

        var nonFatturate = perFoglio["Non Fatturate"];
        Assert.Multiple(() =>
        {
            Assert.That(nonFatturate, Does.Contain("SECONDO SALDO"),
                "Le non fatturate della tipologia del report ci sono (righe statiche del seed).");
            Assert.That(nonFatturate, Does.Not.Contain("PRIMO SALDO"),
                "Le non fatturate delle ALTRE tipologie non entrano nello sheet: se un domani ci "
                + "entrassero, il requisito e' stato riletto come 'elenco unico' — aggiornare il test.");
            Assert.That(nonFatturate, Does.Not.Contain("VAR. SEMESTRALE"));
        });
    }

    /// <summary>
    /// La lettura **letterale** del testbook: un unico elenco con tutte le posticipate ed eliminate,
    /// indipendentemente dalla tipologia del report.
    ///
    /// ATTENZIONE **Da chiarire col prodotto prima di trattarlo come difetto**, per lo stesso motivo detto
    /// sopra: se vale la lettura "per tipologia", questo test va **cancellato** e resta la
    /// caratterizzazione.
    /// </summary>
    [Test]
    [Ignore("REQUISITO DA CHIARIRE — lo sheet 'Non Fatturate' contiene le sole non fatturate della "
        + "tipologia del report (filtro esplicito in FattureExtensions), mentre il testbook parla di "
        + "'tutte le fatture Posticipate ed Eliminate presenti in Gestione Fatture'. Decidere quale "
        + "delle due letture vale; se vale quella attuale, cancellare questo test. "
        + "V. coverage/test-backlog.md.")]
    public async Task Report_LoSheet_ShouldRaccogliereTutteLeTipologie()
    {
        SeminaStaging("PRIMO SALDO", 1);
        SeminaStaging("VAR. SEMESTRALE", 3, stato: 3, azione: "ELIMINATA");

        // Stessa base di confronto della caratterizzazione (il foglio, non l'intero workbook):
        // sull'intero file queste asserzioni passerebbero per il motivo sbagliato, cioe' i fogli di
        // dettaglio dei report delle altre tipologie.
        var (_, _, _, perFoglio) = await Report("SECONDO SALDO");
        var nonFatturate = perFoglio["Non Fatturate"];

        Assert.Multiple(() =>
        {
            Assert.That(nonFatturate, Does.Contain("PRIMO SALDO"));
            Assert.That(nonFatturate, Does.Contain("VAR. SEMESTRALE"));
            Assert.That(nonFatturate, Does.Contain("ELIMINATA"), "Entrambi gli stati, non solo le posticipate.");
        });
    }

    // =============================================================================================
    // `PF-672 TD-20`: lo stesso sheet nel **Report Non Inviate**, scaricato dalla pagina Invia Fatture
    // (`POST api/fatture/pagopa/non-inviate/report`).
    //
    // I due report condividono il generatore `ReportFattureRel` — è il motivo per cui il difetto del
    // 29/07/2026 li riguardava entrambi — ma **partono da dati diversi**: qui non si chiede un periodo,
    // si chiedono le tipologie e si riportano tutte le fatture non inviate. Sul DB seedato questo dà un
    // vantaggio: le tipologie di saldo con fatture non inviate sono **due** (`SECONDO SALDO` e
    // `VAR. SEMESTRALE`), quindi lo sheet si può verificare su due report invece che su uno.
    // =============================================================================================

    [Test]
    public async Task ReportNonInviate_ShouldContenereLoSheetNonFatturate()
    {
        // Il report vuole una fattura NON inviata **completa** (righe + REL corrispondente): sul seed
        // l'unica completa (8001) e' gia' inviata, quindi le parti mancanti della 7501 le aggiunge il
        // test. E' il minimo perche' il report esista: senza, la rotta risponde 404 e lo sheet non si
        // puo' nemmeno cercare.
        CompletaLaFatturaNonInviata();
        SeminaStaging(TipologiaNonInviata, 3, ente: EnteNonInviata);

        var (stato, fogli, _, _) = await ReportNonInviate(TipologiaNonInviata);

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK),
            "Senza report non c'e' sheet: se qui arriva 404, mancano i dati di supporto, non il fix.");
        Assert.That(fogli, Does.Contain("Non Fatturate"),
            "Il fix del 29/07/2026 vale per entrambi i report, che condividono ReportFattureRel: se lo "
            + "sheet manca qui ma c'e' nell'altro, sono tornati a divergere.");
    }

    [Test]
    public async Task ReportNonInviate_SenzaStagingPerQuellaTipologia_ShouldNonAvereLoSheet()
    {
        // Contro-prova possibile qui e non sull'altro report: la tipologia usata non ha righe statiche
        // in cfg.GestioneFatture, quindi l'assenza dello sheet e' significativa.
        CompletaLaFatturaNonInviata();

        var (stato, fogli, _, _) = await ReportNonInviate(TipologiaNonInviata);

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(fogli, Is.Not.Empty, "Il report ha i suoi fogli: il confronto e' significativo.");
        Assert.That(fogli, Does.Not.Contain("Non Fatturate"),
            "Nessuna posticipata ne' eliminata per questa tipologia: lo sheet non va aggiunto.");
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    /// <summary>
    /// Scarica il report e ne restituisce i **nomi dei fogli** e il **testo** di tutti i fogli.
    ///
    /// Il download è uno **zip** che contiene uno o più `.xlsx`, ciascuno a sua volta uno zip Open XML:
    /// si apre quindi due volte. Due dettagli che hanno fatto sbagliare la prima versione:
    ///
    /// - i nomi dei fogli stanno in `xl/workbook.xml` in elementi con **prefisso di namespace**
    ///   (`&lt;x:sheet name="…"&gt;`): una regex su `&lt;sheet` non trova nulla e — peggio — il test
    ///   di contro-prova passa sul vuoto, sembrando verde;
    /// - i testi delle celle sono **inline** nei fogli, non in `xl/sharedStrings.xml` (che non esiste).
    /// </summary>
    private Task<(HttpStatusCode stato, List<string> fogli, string testo,
        Dictionary<string, string> perFoglio)> Report(params string[] tipologie)
    {
        var elenco = string.Join(",", tipologie.Select(t => $"\"{t}\""));
        return Scarica(Rotta, $$"""{ "anno": {{Anno}}, "mese": {{Mese}}, "tipologiaFattura": [{{elenco}}] }""");
    }

    /// <summary>
    /// Il report della pagina Invia Fatture: niente periodo, solo le tipologie e `inviata = 0`.
    /// </summary>
    private Task<(HttpStatusCode stato, List<string> fogli, string testo,
        Dictionary<string, string> perFoglio)> ReportNonInviate(params string[] tipologie)
    {
        var elenco = string.Join(",", tipologie.Select(t => $"\"{t}\""));
        return Scarica(RottaNonInviate, $$"""{ "tipologiaFattura": [{{elenco}}], "inviata": 0 }""");
    }

    private async Task<(HttpStatusCode stato, List<string> fogli, string testo,
        Dictionary<string, string> perFoglio)> Scarica(string rotta, string body)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.PostAsync(_factory.WithNonce(rotta),
            new StringContent(body, Encoding.UTF8, "application/json"));
        TestContext.Out.WriteLine($"{rotta} {body} -> {(int)resp.StatusCode}");

        if (resp.StatusCode != HttpStatusCode.OK) return (resp.StatusCode, [], string.Empty, []);

        var fogli = new List<string>();
        var testo = new StringBuilder();
        var perFoglio = new Dictionary<string, StringBuilder>();
        using var zip = new ZipArchive(new MemoryStream(await resp.Content.ReadAsByteArrayAsync()), ZipArchiveMode.Read);
        foreach (var xlsx in zip.Entries.Where(e => e.FullName.EndsWith(".xlsx")))
        {
            using var buffer = new MemoryStream();
            await xlsx.Open().CopyToAsync(buffer);
            buffer.Position = 0;

            using var interno = new ZipArchive(buffer, ZipArchiveMode.Read);
            var parti = new Dictionary<string, string>();
            foreach (var parte in interno.Entries)
            {
                using var reader = new StreamReader(parte.Open());
                parti[parte.FullName] = await reader.ReadToEndAsync();
            }

            foreach (var parte in parti.Where(x => x.Key.StartsWith("xl/") && x.Key.EndsWith(".xml")))
                testo.Append(parte.Value);

            // Nome del foglio -> contenuto del SOLO foglio: `workbook.xml` da' nome e `r:id`, e i
            // `.rels` traducono l'`r:id` nel file (`xl/worksheets/sheetN.xml`). L'ordine dei file NON
            // e' una scorciatoia affidabile: non segue quello dei fogli.
            // ATTENZIONE l'ordine degli attributi non e' garantito: qui e' Type, Target, Id — una
            // regex che pretendesse Id prima di Target non troverebbe nulla, e il foglio resterebbe
            // fuori dalla mappa senza che niente lo segnali.
            var bersagli = new Dictionary<string, string>();
            if (parti.TryGetValue("xl/_rels/workbook.xml.rels", out var rels))
                foreach (Match rel in Regex.Matches(rels, "<Relationship\\b[^>]*?>"))
                {
                    var id = Regex.Match(rel.Value, "Id=\"([^\"]+)\"").Groups[1].Value;
                    var bersaglio = Regex.Match(rel.Value, "Target=\"([^\"]+)\"").Groups[1].Value;
                    if (id.Length > 0 && bersaglio.Length > 0) bersagli[id] = bersaglio;
                }

            if (!parti.TryGetValue("xl/workbook.xml", out var workbook)) continue;
            foreach (Match sheet in Regex.Matches(workbook, "<(?:\\w+:)?sheet\\b[^>]*?>"))
            {
                var nome = Regex.Match(sheet.Value, "name=\"([^\"]+)\"").Groups[1].Value;
                if (nome.Length == 0) continue;
                fogli.Add(nome);

                var rid = Regex.Match(sheet.Value, "r:id=\"([^\"]+)\"").Groups[1].Value;
                if (!bersagli.TryGetValue(rid, out var target)) continue;
                var percorso = target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
                if (!parti.TryGetValue(percorso, out var xmlFoglio)) continue;

                if (!perFoglio.TryGetValue(nome, out var accumulatore))
                    perFoglio[nome] = accumulatore = new StringBuilder();
                accumulatore.Append(xmlFoglio);
            }
        }

        TestContext.Out.WriteLine($"  fogli: {string.Join(" | ", fogli)}");
        return (resp.StatusCode, fogli, testo.ToString(),
            perFoglio.ToDictionary(x => x.Key, x => x.Value.ToString()));
    }

    /// <summary>
    /// Completa la fattura **non inviata** 7501 (`VAR. SEMESTRALE` 2026/7, ente dedicato) con ciò che il
    /// report pretende: una **riga** e la **REL** corrispondente. Sul seed nessuna fattura non inviata
    /// le ha entrambe — l'unica completa, la 8001, è già inviata e quindi fuori da questo report.
    ///
    /// ATTENZIONE `pfd.RelTestata` vuole le colonne `Asseverazione*` valorizzate: sono `decimal`/`int` **non
    /// nullable** sul DTO, e un NULL fa fallire il mapping con un 500 che sembra un problema di vista.
    /// </summary>
    private static void CompletaLaFatturaNonInviata() => Esegui($@"
        IF NOT EXISTS (SELECT 1 FROM pfd.FattureRighe WHERE FkIdFattura = {IdFatturaNonInviata})
        INSERT INTO pfd.FattureRighe
            (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile,
             RigaBollo, PeriodoRiferimento)
        VALUES ({IdFatturaNonInviata}, 1, 'riga report', 'MAT-A', 1, 2000.00, 2000.00, 0, '07/2026');

        IF NOT EXISTS (SELECT 1 FROM pfd.RelTestata
                       WHERE internal_organization_id='{EnteNonInviata}' AND [year]=2026 AND [month]=7)
        INSERT INTO pfd.RelTestata
            (internal_organization_id, contract_id, TipologiaFattura, [year], [month],
             TotaleAnalogico, TotaleDigitale, TotaleNotificheAnalogiche, TotaleNotificheDigitali,
             Totale, TotaleAnalogicoIva, TotaleDigitaleIva, TotaleIva, Caricata, RelFatturata,
             AsseverazioneTotaleAnalogico, AsseverazioneTotaleDigitale,
             AsseverazioneTotaleNotificheAnalogiche, AsseverazioneTotaleNotificheDigitali,
             AsseverazioneTotale, AsseverazioneTotaleAnalogicoIva, AsseverazioneTotaleDigitaleIva,
             AsseverazioneTotaleIva)
        VALUES ('{EnteNonInviata}', 'TOKEN-E7', '{TipologiaNonInviata}', 2026, 7,
                1000.00, 1000.00, 10, 10, 2000.00, 1220.00, 1220.00, 2440.00, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0);");

    private static void SeminaStaging(string tipologia, int mese, int stato = 0,
        string azione = "POSTICIPATA", string? ente = null) => Esegui($@"
        IF NOT EXISTS (SELECT 1 FROM cfg.GestioneFatture
                       WHERE FkIdEnte='{ente ?? IdEnte}' AND FkTipologiaFattura='{tipologia}'
                         AND Anno={AnnoStaging} AND Mese={mese})
        INSERT INTO cfg.GestioneFatture
            (FkIdEnte, FkTipologiaFattura, Anno, Mese, DataInserimento, IdUtenteInserimento, Stato, Azione, Note)
        VALUES ('{ente ?? IdEnte}', '{tipologia}', {AnnoStaging}, {mese}, GETDATE(), 'integration-test-user',
                {stato}, '{azione}', N'[]');");

    private static void Pulisci() => Esegui($@"
        DELETE FROM cfg.GestioneFatture WHERE FkIdEnte='{IdEnte}' AND Anno={AnnoStaging};
        DELETE FROM cfg.GestioneFatture WHERE FkIdEnte='{EnteNonInviata}' AND Anno={AnnoStaging};
        DELETE FROM pfd.FattureRighe WHERE FkIdFattura = {IdFatturaNonInviata};
        DELETE FROM pfd.RelTestata
            WHERE internal_organization_id='{EnteNonInviata}' AND [year]=2026 AND [month]=7;");

    private static void Esegui(string sql)
    {
        try
        {
            using var conn = new SqlConnection(LocalTestDb.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { /* best-effort */ }
    }
}
