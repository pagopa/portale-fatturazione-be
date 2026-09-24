using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// PF-882, seconda meta': da quale periodo vengono le colonne `anno`/`mese` del "Report di dettaglio
/// notifiche". Fino al 21/09/2026 erano `r.year`/`r.month`, cioe' il periodo della REL; ora sono
/// `AnnoNotifica`/`MeseNotifica`, cioe' il periodo di competenza della **notifica**.
///
/// Il filtro NON e' cambiato e resta su `r.year`/`r.month` (`RelRigheQueryGetByIdPersistence`). La
/// conseguenza e' un'asimmetria che vale la pena avere sotto test, perche' non si vede leggendo il
/// solo SELECT: **il file seleziona per periodo di fatturazione e mostra il periodo di competenza**.
///
/// Il seed condiviso non aiuta qui: `tests/Data/gestione_fatture.sql` dichiara le due colonne nella
/// DDL ma non le valorizza in nessun INSERT, quindi su quelle righe la modifica e' invisibile (le
/// colonne escono vuote e nulla diventa rosso). Questa fixture si costruisce percio' **un periodo
/// tutto suo**, 2029/10, in cui i due periodi sono DIVERSI di proposito: e' l'unico modo di
/// distinguere i due comportamenti.
///
/// Periodo creato in SetUp e distrutto in TearDown invece che aggiunto al seed: una `pfd.RelTestata`
/// in piu' su un periodo condiviso cambia l'esito dei test dell'Orchestratore e dei report sospese
/// (v. `docs/test-integrazione-db-seedato.md`, § periodi riservati).
/// </summary>
public class RelRigheAnnoNotificaIntegrationTests
{
    private const string Ente = "11111111-1111-1111-1111-111111111111";
    private const string Contratto = "TOKEN-E1";
    private const string Tipologia = "SECONDO SALDO";

    // Periodo della REL: e' quello su cui si filtra.
    private const int AnnoRel = 2029;
    private const int MeseRel = 10;

    private string _cn = null!;
    private IMediator _handler = null!;

    // Il TearDown deve sapere se c'e' davvero qualcosa da ripulire: con il container spento lo
    // SkipIfUnavailable interrompe il SetUp con un Assert.Ignore, ma NUnit esegue comunque il
    // TearDown — che senza questa guardia proverebbe a connettersi, fallirebbe, e trasformerebbe
    // cinque test *ignorati* in cinque test *rossi*, per il motivo sbagliato.
    private bool _seedPronto;

    [SetUp]
    public void Setup()
    {
        _seedPronto = false;
        _cn = LocalTestDb.ConnectionString;
        TestDb.SkipIfUnavailable(_cn);
        _handler = ServiceProvider.GetRequiredService<IMediator>(_cn);

        Pulisci();
        CreaTestata();

        // Tre righe DENTRO il periodo REL 2029/10, con periodo di notifica diverso fra loro:
        CreaRiga("REL-AN-1", MeseRel, annoNotifica: 2029, meseNotifica: 8);
        CreaRiga("REL-AN-2", MeseRel, annoNotifica: 2028, meseNotifica: 12);
        CreaRiga("REL-AN-NULL", MeseRel, annoNotifica: null, meseNotifica: null);

        // ...e una FUORI dal periodo REL, ma la cui NOTIFICA cade proprio in 2029/10: e' la riga che
        // uscirebbe se qualcuno spostasse anche il filtro sulle colonne della notifica.
        CreaRiga("REL-AN-FUORI", mese: 7, annoNotifica: AnnoRel, meseNotifica: MeseRel);

        _seedPronto = true;
    }

    [TearDown]
    public void TearDown()
    {
        if (_seedPronto)
            Pulisci();
    }

    // ---------------------------------------------------------------------------------------------
    // Il comportamento introdotto da PF-882
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Le colonne esposte sono quelle della notifica. Su `REL-AN-1` la REL e' di ottobre 2029 e la
    /// notifica di agosto 2029: il DTO deve dire **agosto**.
    /// </summary>
    [Test]
    public async Task Righe_ShouldEsporreIlPeriodoDellaNotificaNonQuelloDellaRel()
    {
        var righe = await Righe();
        var riga = righe.Single(x => x.IdNotifica == "REL-AN-1");

        Assert.Multiple(() =>
        {
            Assert.That(riga.Anno, Is.EqualTo("2029"));
            Assert.That(riga.Mese, Is.EqualTo("8"), "agosto e' il mese della notifica; ottobre e' quello della REL");
        });
    }

    /// <summary>
    /// L'asimmetria, provata nei due versi: entra `REL-AN-2` (REL nel periodo, notifica di dicembre
    /// 2028) e NON entra `REL-AN-FUORI` (REL di luglio, notifica proprio in 2029/10). Se un domani il
    /// filtro seguisse le colonne della notifica, questo test fallirebbe da entrambe le parti.
    /// </summary>
    [Test]
    public async Task Filtro_ShouldRestarePerPeriodoDellaRel_NonPerPeriodoDellaNotifica()
    {
        var idNotifiche = (await Righe()).Select(x => x.IdNotifica).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(idNotifiche, Is.EquivalentTo(new[] { "REL-AN-1", "REL-AN-2", "REL-AN-NULL" }));
            Assert.That(idNotifiche, Has.None.EqualTo("REL-AN-FUORI"),
                "la sua notifica cade nel periodo richiesto, ma la sua REL no: non deve uscire");
        });
    }

    /// <summary>
    /// Il file che l'aderente riceve. Nota che le intestazioni si chiamano ancora `year` e `month`
    /// (la mappa CsvHelper non e' stata toccata): il nome della colonna non dice che la semantica e'
    /// cambiata, ed e' il motivo per cui la cosa va scritta da qualche parte oltre che testata.
    /// </summary>
    [Test]
    public async Task Csv_ShouldRiportareIlPeriodoDellaNotificaNelleColonneYearEMonth()
    {
        var csv = await Csv();
        var (anno, mese) = PeriodoNelCsv(csv, "REL-AN-2");

        Assert.Multiple(() =>
        {
            Assert.That(anno, Is.EqualTo("2028"));
            Assert.That(mese, Is.EqualTo("12"));
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Avversariali
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// CARATTERIZZAZIONE, ed e' il rischio pratico della modifica. Le due colonne sono nullable a DB e
    /// il DTO le espone come `string?`: se la pipeline non le valorizza, la riga esce lo stesso e il
    /// periodo e' semplicemente **vuoto**. Nessuna eccezione, nessun log, nessun segnale — chi legge
    /// il CSV vede due celle bianche.
    ///
    /// E' esattamente lo stato in cui si trova oggi il resto del seed, dove quelle colonne non sono
    /// popolate: se un domani il dato mancasse anche in produzione, il sintomo sarebbe questo.
    /// </summary>
    [Test]
    public async Task AnnoNotificaNull_ShouldProdurreColonneVuoteSenzaErrore_Caratterizzazione()
    {
        var righe = await Righe();
        var riga = righe.Single(x => x.IdNotifica == "REL-AN-NULL");

        Assert.Multiple(() =>
        {
            Assert.That(riga.Anno, Is.Null);
            Assert.That(riga.Mese, Is.Null);
        });
    }

    /// <summary>Stessa cosa vista dal file: due celle vuote, non la stringa "null".</summary>
    [Test]
    public async Task Csv_AnnoNotificaNull_ShouldLasciareLeCelleVuote_Caratterizzazione()
    {
        var csv = await Csv();
        var (anno, mese) = PeriodoNelCsv(csv, "REL-AN-NULL");

        Assert.Multiple(() =>
        {
            Assert.That(anno, Is.Empty);
            Assert.That(mese, Is.Empty);
        });
    }

    // ---------------------------------------------------------------------------------------------

    private async Task<List<RigheRelDto>> Righe()
    {
        var chiave = $"{Ente}_{Contratto}_{Tipologia.Replace(" ", "-")}_{AnnoRel}_{MeseRel}";
        var righe = await _handler.Send(new RelRigheQueryGetById(Auth()) { IdTestata = chiave });
        return righe!.ToList();
    }

    private async Task<string> Csv()
    {
        var stream = await (await Righe()).ToStream<RigheRelDto, RigheRelDtoPagoPAMap>();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Legge le colonne `year`/`month` del CSV risalendo alla loro posizione dall'intestazione,
    /// invece di fissare due indici: la mappa `RigheRelDtoPagoPAMap` ha dei buchi negli `Index`, e
    /// una posizione hardcoded si romperebbe alla prima colonna aggiunta.
    /// </summary>
    private static (string Anno, string Mese) PeriodoNelCsv(string csv, string idNotifica)
    {
        var righe = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var intestazione = righe[0].Split(';');
        var iAnno = Array.IndexOf(intestazione, "year");
        var iMese = Array.IndexOf(intestazione, "month");

        Assert.That(iAnno, Is.GreaterThanOrEqualTo(0), "colonna 'year' assente dall'intestazione del CSV");
        Assert.That(iMese, Is.GreaterThanOrEqualTo(0), "colonna 'month' assente dall'intestazione del CSV");

        var riga = righe.Single(r => r.Contains(idNotifica)).Split(';');
        return (riga[iAnno], riga[iMese]);
    }

    private void CreaTestata() => Esegui(
        @"INSERT INTO pfd.RelTestata
            (internal_organization_id, contract_id, TipologiaFattura, [year], [month],
             TotaleAnalogico, TotaleDigitale, TotaleNotificheAnalogiche, TotaleNotificheDigitali,
             Totale, TotaleAnalogicoIva, TotaleDigitaleIva, TotaleIva, Caricata, RelFatturata,
             AsseverazioneTotaleAnalogico, AsseverazioneTotaleDigitale,
             AsseverazioneTotaleNotificheAnalogiche, AsseverazioneTotaleNotificheDigitali,
             AsseverazioneTotale, AsseverazioneTotaleAnalogicoIva, AsseverazioneTotaleDigitaleIva,
             AsseverazioneTotaleIva)
          VALUES (@ente, @contratto, @tipologia, @anno, @mese,
                  0, 3.00, 0, 3, 3.00, 0, 0.66, 0.66, 0, 0,
                  0, 0, 0, 0, 0, 0, 0, 0);",
        // Le colonne Asseverazione* sono valorizzate di proposito: sul DTO di lettura sono
        // decimal/int non nullable, e un NULL fa fallire il mapping Dapper con un errore che sembra
        // un problema di vista e non lo e'.
        cmd =>
        {
            cmd.Parameters.AddWithValue("@ente", Ente);
            cmd.Parameters.AddWithValue("@contratto", Contratto);
            cmd.Parameters.AddWithValue("@tipologia", Tipologia);
            cmd.Parameters.AddWithValue("@anno", AnnoRel);
            cmd.Parameters.AddWithValue("@mese", MeseRel);
        });

    private void CreaRiga(string eventId, int mese, int? annoNotifica, int? meseNotifica) => Esegui(
        @"INSERT INTO pfd.RelRighe
            (contract_id, tax_code, vat_number, event_id, iun, internal_organization_id,
             [year], [month], item_code, notification_request_id, recipient_tax_id, notificationtype,
             cost, TipologiaFattura, IdFlagContestazione, FlagConguaglio, AnnoNotifica, MeseNotifica)
          VALUES (@contratto, 'TAX-AN', 'VAT-AN', @eventId, @iun, @ente,
                  @anno, @mese, 'IC', 'NRQ', 'RTX', 'Digitali',
                  1.00, @tipologia, 1, NULL, @annoNotifica, @meseNotifica);",
        cmd =>
        {
            cmd.Parameters.AddWithValue("@contratto", Contratto);
            cmd.Parameters.AddWithValue("@eventId", eventId);
            cmd.Parameters.AddWithValue("@iun", "IUN-" + eventId);
            cmd.Parameters.AddWithValue("@ente", Ente);
            cmd.Parameters.AddWithValue("@anno", AnnoRel);
            cmd.Parameters.AddWithValue("@mese", mese);
            cmd.Parameters.AddWithValue("@tipologia", Tipologia);
            cmd.Parameters.AddWithValue("@annoNotifica", (object?)annoNotifica ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@meseNotifica", (object?)meseNotifica ?? DBNull.Value);
        });

    private void Pulisci()
    {
        Esegui("DELETE FROM pfd.RelRighe WHERE event_id LIKE 'REL-AN-%';", _ => { });
        Esegui("DELETE FROM pfd.RelTestata WHERE [year] = @anno AND [month] = @mese AND contract_id = @contratto;",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@anno", AnnoRel);
                cmd.Parameters.AddWithValue("@mese", MeseRel);
                cmd.Parameters.AddWithValue("@contratto", Contratto);
            });
    }

    private void Esegui(string sql, Action<SqlCommand> parametri)
    {
        using var conn = new SqlConnection(_cn);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        parametri(cmd);
        cmd.ExecuteNonQuery();
    }

    private static AuthenticationInfo Auth() => new()
    {
        Id = "integration-test-rel-annonotifica",
        IdEnte = Ente,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };
}
