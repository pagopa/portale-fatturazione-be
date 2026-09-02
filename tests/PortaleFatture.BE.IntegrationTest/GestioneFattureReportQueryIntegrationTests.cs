using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test su GestioneFattureReportQuery (MediatR -> persistence -> vista be.vwGestioneFattureReport)
/// contro il DB locale seeded. Verifica il MAPPING della vista sul DTO e i punti a rischio individuati:
///   - Dapper con "IN @TipologiaFattura" e lista null (report senza filtro: il caso usato da ReportFatture);
///   - float -> decimal? su TotaleFatturaImponibile;
///   - la colonna Stato (= gf.Azione, stringa) e il filtro IN (0,3) della vista;
///   - il caso reale "imponibili valorizzati ma IVA NULL".
///
/// I dati NON stanno nel seed condiviso: li semina questa fixture su un ENTE gia' presente
/// (44444444...) in un PERIODO dedicato (anno 2099), e li ripulisce in Set/TearDown. Motivo: la PK
/// composta di cfg.GestioneFatture rende una riga dimenticata un problema per gli altri test.
/// Richiede il container attivo (tests/: docker compose up -d --build).
/// </summary>
public class GestioneFattureReportQueryIntegrationTests
{
    // Ente presente nel seed (dati_fatturazione.sql) con Enti + Contratti: la vista fa INNER JOIN su
    // entrambi, quindi serve un ente reale, non un Guid casuale.
    private const string Ente = "44444444-4444-4444-4444-444444444444";
    private const string RagioneSocialeAttesa = "Ente Dati Fatturazione";
    private const int Anno = 2099; // periodo riservato a questi test

    private IMediator _handler;
    private string Conn => LocalTestDb.ConnectionString;

    [SetUp]
    public void Setup()
    {
        // Container locale spento -> test ignorati (warning), non falliti.
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
        Pulisci();
        SeminaScenari();
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

    // Manda la query filtrando i risultati sull'ente/periodo di questa fixture (la vista non filtra per
    // ente: e' un report globale, quindi isoliamo lato client per asserire sui nostri dati).
    private async Task<List<GestioneFattureReportDto>> Report(string[]? tipologie = null)
    {
        var res = await _handler.Send(new GestioneFattureReportQuery(AdminAuth()) { TipologiaFattura = tipologie });
        return (res ?? []).Where(r => r.IdEnte == Ente && r.Anno == Anno).ToList();
    }

    private GestioneFattureReportDto? Riga(List<GestioneFattureReportDto> rows, string tipologia, int mese)
        => rows.FirstOrDefault(r => r.TipologiaFattura == tipologia && r.Mese == mese);

    // ---------------------------------------------------------------------------------------------
    // 1) Il caso che ReportFatture usa davvero: nessun filtro tipologia -> TipologiaFattura = null.
    //    Serve a smascherare l'espansione Dapper di "IN @TipologiaFattura" con parametro null.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Report_SenzaFiltroTipologia_NonEsplode_ERitornaLeRighe()
    {
        var rows = await Report(tipologie: null);
        Assert.That(rows, Is.Not.Empty,
            "Il report senza filtro deve ritornare le righe seminate (POSTICIPATA + ELIMINATA). "
          + "Se qui arriva un'eccezione Dapper, e' l'espansione di 'IN @TipologiaFattura' con lista null: "
          + "in Persistence passare 'TipologiaFattura = tipoFattura ?? Array.Empty<string>()'.");
    }

    // ---------------------------------------------------------------------------------------------
    // 2) MAPPING completo sul caso valorizzato (match Fattura + Rel), con IVA NULL come nei dati reali.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Report_CasoValorizzato_MappaTutteLeColonne()
    {
        var r = Riga(await Report(), "PRIMO SALDO", 2);
        Assert.That(r, Is.Not.Null, "La riga POSTICIPATA valorizzata deve comparire.");

        Assert.Multiple(() =>
        {
            Assert.That(r!.IdEnte, Is.EqualTo(Ente));
            Assert.That(r.RagioneSociale, Is.EqualTo(RagioneSocialeAttesa));
            Assert.That(r.TipologiaFattura, Is.EqualTo("PRIMO SALDO"));
            Assert.That(r.NumeroFattura, Is.EqualTo(2099000002L), "bigint: mappato su long?");
            Assert.That(r.TipoDocumento, Is.EqualTo("TD01"));
            Assert.That(r.Anno, Is.EqualTo(2099));
            Assert.That(r.Mese, Is.EqualTo(2));
            Assert.That(r.TotaleNotificheAnalogiche, Is.EqualTo(7));
            Assert.That(r.TotaleNotificheDigitali, Is.EqualTo(8));
            Assert.That(r.TotaleNotifiche, Is.EqualTo(15), "somma calcolata nella vista");
            Assert.That(r.TotaleImponibileAnalogico, Is.EqualTo(10.50m));
            Assert.That(r.TotaleImponibileDigitale, Is.EqualTo(20.25m));
            Assert.That(r.TotaleImponibile, Is.EqualTo(30.75m));
            // caso reale: imponibili valorizzati ma IVA a NULL (RelTestata.*Iva null)
            Assert.That(r.TotaleIvatoAnalogico, Is.Null);
            Assert.That(r.TotaleIvatoDigitale, Is.Null);
            Assert.That(r.TotaleIvato, Is.Null);
            Assert.That(r.Firmata, Is.EqualTo("Firmata"), "Caricata=1 -> 'Firmata'");
            // float -> decimal?: tolleranza perche' la sorgente e' float e puo' avere coda binaria
            Assert.That(r.TotaleFatturaImponibile, Is.EqualTo(1234.56m).Within(0.01m),
                "float SQL mappato su decimal?: se qui esplode, CAST(... AS decimal(18,2)) nel builder.");
            Assert.That(r.TipoContratto, Is.Not.Null.And.Not.Empty);
            Assert.That(r.Stato, Is.EqualTo("POSTICIPATA"), "colonna Stato = gf.Azione (stringa)");
        });
    }

    // ---------------------------------------------------------------------------------------------
    // 3) Caso senza fattura ancora generata: tutti i totali NULL (la forma piu' comune nei dati reali).
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Report_CasoSenzaFattura_TotaliNull_EFirmataNonCaricata()
    {
        var r = Riga(await Report(), "SECONDO SALDO", 1);
        Assert.That(r, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(r!.NumeroFattura, Is.Null);
            Assert.That(r.TipoDocumento, Is.Null);
            Assert.That(r.TotaleNotifiche, Is.Null);
            Assert.That(r.TotaleImponibile, Is.Null);
            Assert.That(r.TotaleFatturaImponibile, Is.Null);
            Assert.That(r.Firmata, Is.EqualTo("Non Caricata"), "nessun match Rel -> ELSE del CASE");
            Assert.That(r.Stato, Is.EqualTo("POSTICIPATA"));
        });
    }

    // ---------------------------------------------------------------------------------------------
    // 4) Colonna Stato + filtro IN (0,3): l'ELIMINATA compare, la CANCELLATA no.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Report_Eliminata_Compare_ECancellata_No()
    {
        var rows = await Report();
        Assert.That(Riga(rows, "ANTICIPO", 3)?.Stato, Is.EqualTo("ELIMINATA"),
            "Stato=3 -> Azione 'ELIMINATA', ammessa dal filtro IN (0,3).");
        Assert.That(Riga(rows, "ACCONTO", 4), Is.Null,
            "Stato=2 (CANCELLATA) e' escluso dalla vista.");
    }

    // ---------------------------------------------------------------------------------------------
    // 5) Filtro per tipologia: ritorna solo quella richiesta.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Report_ConFiltroTipologia_RitornaSoloQuella()
    {
        var rows = await Report(tipologie: ["PRIMO SALDO"]);
        Assert.That(rows, Is.Not.Empty);
        Assert.That(rows.Select(r => r.TipologiaFattura), Is.All.EqualTo("PRIMO SALDO"),
            "Con il filtro devono restare solo le righe PRIMO SALDO.");
    }

    // ---------------------------------------------------------------------------------------------
    // 6) Filtro multi-tipologia: nel CSV convivono PRIMO/SECONDO SALDO, ANTICIPO, SEM. SOSPESI.
    //    Il filtro con piu' valori deve tornare l'unione di quelle tipologie e nulla di piu'.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Report_ConFiltroMultiTipologia_RitornaSoloQuelle()
    {
        var rows = await Report(tipologie: ["PRIMO SALDO", "ANTICIPO"]);
        Assert.That(rows, Is.Not.Empty);
        Assert.That(rows.Select(r => r.TipologiaFattura).Distinct(),
            Is.EquivalentTo(new[] { "PRIMO SALDO", "ANTICIPO" }),
            "Con il filtro a due valori devono comparire solo PRIMO SALDO ed ANTICIPO (non SECONDO SALDO).");
    }

    // ---------------------------------------------------------------------------------------------
    // 7) NumeroFattura oltre Int32: nel CSV i progressivi sono a 10 cifre (es. 2607032716 > 2^31).
    //    Blinda la scelta long?: con int? qui ci sarebbe overflow.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Report_NumeroFatturaOltreInt32_MappaSenzaOverflow()
    {
        const long progressivoGrande = 2607032716L; // valore reale dal CSV di dev, > int.MaxValue
        InsertGestioneFatture("VAR. SEMESTRALE", 5, stato: 0, azione: "POSTICIPATA");
        InsertFattura("VAR. SEMESTRALE", 5, progressivo: progressivoGrande, totaleFattura: 99.90, tipoDoc: "TD01");

        var r = Riga(await Report(), "VAR. SEMESTRALE", 5);
        Assert.That(r, Is.Not.Null);
        Assert.That(r!.NumeroFattura, Is.EqualTo(progressivoGrande),
            "Progressivo a 10 cifre: mappato integro su long? (int? andrebbe in overflow).");
    }

    // ---------------------------------------------------------------------------------------------
    // 8) Solo Stato 0 e 3 per il nostro periodo: A(0) + B(0) + C(3) = 3; la CANCELLATA(2) esclusa.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Report_SoloRigheStato0e3_PerIlNostroPeriodo()
    {
        var rows = await Report();
        Assert.That(rows, Has.Count.EqualTo(3),
            "Il filtro IN (0,3) della vista deve escludere la sola CANCELLATA seminata (Stato=2).");
        Assert.That(rows.Select(r => r.Stato), Is.All.AnyOf("POSTICIPATA", "ELIMINATA"));
    }

    // ---------------------------------------------------------------------------------------------
    // 9) CARATTERIZZAZIONE del DISTINCT: se per lo stesso periodo esistono DUE fatture in
    //    FattureTestata (progressivi diversi), la vista fa fan-out sul LEFT JOIN. Il DISTINCT non
    //    ricompatta righe che differiscono per NumeroFattura -> il report le mostra entrambe.
    //    Il test fissa il comportamento attuale: se un giorno cambia (dedup su periodo), va aggiornato.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Report_DuePerLoStessoPeriodo_LaVistaLeMostraEntrambe()
    {
        // oltre alla fattura del caso B (PRIMO SALDO/2, progressivo 2099000002) ne aggiungo una seconda
        InsertFattura("PRIMO SALDO", 2, progressivo: 2099000099L, totaleFattura: 500.00, tipoDoc: "TD01");

        var righe = (await Report()).Where(r => r.TipologiaFattura == "PRIMO SALDO" && r.Mese == 2).ToList();
        Assert.That(righe, Has.Count.EqualTo(2),
            "Due fatture sullo stesso periodo -> due righe nel report (il DISTINCT non dedup su periodo). "
          + "Comportamento attuale caratterizzato: se il requisito e' 'una riga per periodo', e' un difetto "
          + "della vista da segnalare, non del DTO.");
        Assert.That(righe.Select(r => r.NumeroFattura), Is.EquivalentTo(new long?[] { 2099000002L, 2099000099L }));
    }

    // ---------------------------------------------------------------------------------------------
    // 10) EDGE: filtro con tipologia inesistente -> 0 righe (nessun errore).
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Report_FiltroTipologiaInesistente_RitornaZero()
    {
        var rows = await Report(tipologie: ["NON ESISTE"]);
        Assert.That(rows, Is.Empty, "Una tipologia inesistente non deve dare match, senza errori.");
    }

    // ---------------------------------------------------------------------------------------------
    // 11) EDGE: array vuoto [] (non null) -> FilterByTipologia=0 -> deve tornare TUTTE, non zero.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Report_FiltroArrayVuoto_RitornaTutte()
    {
        var rows = await Report(tipologie: Array.Empty<string>());
        Assert.That(rows, Is.Not.Empty,
            "Array vuoto: .Any()==false -> FilterByTipologia=0 -> nessun filtro, tutte le righe. "
          + "Se qui torna 0, il ramo 'IN @TipologiaFattura' viene valutato per errore su lista vuota.");
    }

    // ---------------------------------------------------------------------------------------------
    // 12) EDGE: Firmata='Non Caricata' anche CON match Rel, quando Caricata=0 (non solo senza match).
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Report_ConRelMaCaricataZero_Firmata_NonCaricata()
    {
        InsertGestioneFatture("VAR. SEMESTRALE", 6, stato: 0, azione: "POSTICIPATA");
        InsertFattura("VAR. SEMESTRALE", 6, progressivo: 2099000006L, totaleFattura: 50.00, tipoDoc: "TD01");
        InsertRel("VAR. SEMESTRALE", 6, notAnalog: 1, notDigit: 1, impAnalog: 1m, impDigit: 1m, caricata: 0);

        var r = Riga(await Report(), "VAR. SEMESTRALE", 6);
        Assert.That(r, Is.Not.Null);
        Assert.That(r!.Firmata, Is.EqualTo("Non Caricata"),
            "Caricata=0 con match Rel -> 'Non Caricata' (il CASE guarda il valore, non l'esistenza del join).");
        Assert.That(r.TotaleImponibile, Is.EqualTo(2m), "il match Rel c'e': gli imponibili sono valorizzati");
    }

    // ---------------------------------------------------------------------------------------------
    // 13) ADVERSARIAL: SQL injection nel filtro tipologia -> valore, non SQL. Tabella intatta.
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Report_SqlInjectionInTipologia_TrattataComeValore_TabellaIntatta()
    {
        var evil = "PRIMO SALDO'; DROP TABLE cfg.GestioneFatture; --";
        var rows = await Report(tipologie: [evil]);

        Assert.That(rows, Is.Empty, "La stringa malevola non matcha nessuna tipologia: 0 righe, nessuna injection.");
        Assert.That(TabellaEsiste("GestioneFatture"), Is.True, "cfg.GestioneFatture deve essere intatta.");
    }

    // ---------------------------------------------------------------------------------------------
    // 14) ADVERSARIAL: array con NULL dentro -> Dapper lo espande, non deve crashare (al piu' no-match).
    // ---------------------------------------------------------------------------------------------
    [Test]
    public async Task Report_FiltroConNullNellArray_NonCrasha()
    {
        var rows = await Report(tipologie: ["PRIMO SALDO", null!]);
        // 'PRIMO SALDO' deve comunque matchare; il null semplicemente non matcha.
        Assert.That(rows.Select(r => r.TipologiaFattura), Is.All.EqualTo("PRIMO SALDO"));
    }

    private bool TabellaEsiste(string nome)
    {
        using var conn = new SqlConnection(Conn); conn.Open();
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name=@n", conn);
        cmd.Parameters.AddWithValue("@n", nome);
        return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
    }

    // ---------------------------------------------------------------------------------------------
    // seed / cleanup
    // ---------------------------------------------------------------------------------------------
    private void SeminaScenari()
    {
        // A: POSTICIPATA senza fattura -> tutti NULL, Firmata 'Non Caricata'
        InsertGestioneFatture("SECONDO SALDO", 1, stato: 0, azione: "POSTICIPATA");

        // B: POSTICIPATA con FattureTestata + RelTestata (imponibili valorizzati, IVA NULL, Caricata=1)
        InsertGestioneFatture("PRIMO SALDO", 2, stato: 0, azione: "POSTICIPATA");
        InsertFattura("PRIMO SALDO", 2, progressivo: 2099000002L, totaleFattura: 1234.56, tipoDoc: "TD01");
        InsertRel("PRIMO SALDO", 2, notAnalog: 7, notDigit: 8, impAnalog: 10.50m, impDigit: 20.25m, caricata: 1);

        // C: ELIMINATA (Stato=3) -> deve comparire
        InsertGestioneFatture("ANTICIPO", 3, stato: 3, azione: "ELIMINATA");

        // negativo: CANCELLATA (Stato=2) -> NON deve comparire
        InsertGestioneFatture("ACCONTO", 4, stato: 2, azione: "CANCELLATA");
    }

    private void InsertGestioneFatture(string tipologia, int mese, int stato, string azione) => Exec(@"
        INSERT INTO cfg.GestioneFatture
            (FkIdEnte, FkTipologiaFattura, Anno, Mese, DataInserimento, IdUtenteInserimento, Stato, Azione, Note)
        VALUES (@e, @t, @a, @m, GETDATE(), 'itest-report', @s, @az,
                N'{""Data"":""2099-01-01T00:00:00"",""Testo"":""seed-report""}')",
        ("@e", Ente), ("@t", tipologia), ("@a", Anno), ("@m", mese), ("@s", stato), ("@az", azione));

    private void InsertFattura(string tipologia, int mese, long progressivo, double totaleFattura, string tipoDoc) => Exec(@"
        INSERT INTO pfd.FattureTestata
            (FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura, IdentificativoFattura,
             TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento, MeseRiferimento, Progressivo, FatturaInviata)
        VALUES ('prod-pn', @td, @t, @e, '2099-01-01', CONCAT('IT-', @p), @tot, 'EUR', 'MP5', @a, @m, @p, 0)",
        ("@td", tipoDoc), ("@t", tipologia), ("@e", Ente), ("@tot", totaleFattura),
        ("@a", Anno), ("@m", mese), ("@p", progressivo));

    private void InsertRel(string tipologia, int mese, int notAnalog, int notDigit, decimal impAnalog, decimal impDigit, byte caricata) => Exec(@"
        INSERT INTO pfd.RelTestata
            (internal_organization_id, contract_id, TipologiaFattura, [year], [month],
             TotaleNotificheAnalogiche, TotaleNotificheDigitali, TotaleAnalogico, TotaleDigitale,
             Iva, TotaleAnalogicoIva, TotaleDigitaleIva, Caricata, RelFatturata)
        VALUES (@e, CONCAT('ctr-', @a, '-', @m), @t, @a, @m,
                @na, @nd, @ia, @id, 22, NULL, NULL, @c, 0)",
        ("@e", Ente), ("@t", tipologia), ("@a", Anno), ("@m", mese),
        ("@na", notAnalog), ("@nd", notDigit), ("@ia", impAnalog), ("@id", impDigit), ("@c", (int)caricata));

    private void Pulisci()
    {
        Exec("DELETE FROM cfg.GestioneFatture WHERE FkIdEnte=@e AND Anno=@a", ("@e", Ente), ("@a", Anno));
        Exec("DELETE FROM pfd.FattureTestata WHERE FkIdEnte=@e AND AnnoRiferimento=@a", ("@e", Ente), ("@a", Anno));
        Exec("DELETE FROM pfd.RelTestata WHERE internal_organization_id=@e AND [year]=@a", ("@e", Ente), ("@a", Anno));
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
        catch (SqlException) { /* best-effort: setup/cleanup non deve mascherare il fallimento del test */ }
    }
}
