using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Services;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Flusso PEC "Regolare Esecuzione" (Azure Function SendEmail), che prima d'ora non aveva alcuna
/// copertura: ne' la query dei destinatari ne' le due scritture.
///
/// Girano sul DB SEEDATO locale, non su UAT — a differenza del gemello EmailPspServiceIntegrationTests,
/// che e' [Explicit] perche' dipende da dati reali. Qui i dati sono nel seed (tests/Data/email_rel.sql
/// e views/93_EmailRel.sql), quindi i test sono deterministici e si eseguono in un giro normale; se il
/// container e' spento si auto-ignorano.
///
/// Cosa proteggono, in ordine di importanza:
///  1. il filtro PAC: la query esclude gli enti PAL con un INNER JOIN + WHERE tp.Descrizione = 'PAC'.
///     E' una regola di business, non un dettaglio: un PAL non riceve la PEC di Regolare Esecuzione.
///     Sparirebbe in silenzio riscrivendo la query, e nessuno se ne accorgerebbe fino a una mancata
///     comunicazione in produzione;
///  2. la separazione fra anteprima e invio reale: preview scrive su stg.RelEmailPreview con Invio = 0
///     e NON tocca pfd.RelEmail. E' la garanzia su cui si regge la possibilita' di provare il flusso
///     senza spedire nulla;
///  3. la cascata con cui si risolve la PEC, che ha tre sorgenti con precedenze non ovvie.
/// </summary>
public class EmailRelServiceIntegrationTests
{
    // Periodo gia' presente nel seed: ente1 (PAC) ha una pfd.RelTestata SECONDO SALDO 2026/2 con
    // Totale = 300,00 e una PEC in pfd.RelPecEmail.
    private const string EnteSeedPac = "11111111-1111-1111-1111-111111111111";
    private const string EntePal = "33333333-3333-3333-3333-333333333333";
    private const int AnnoSeed = 2026;
    private const int MeseSeed = 2;
    private const string TipologiaSeed = "SECONDO SALDO";

    // Periodo tutto di questa classe, creato e distrutto dai test: non tocca il seed condiviso, che
    // e' incrociato da piu' aree (una RelTestata in piu' su un periodo sbagliato cambia l'esito dei
    // test dell'Orchestratore e dei report sospese).
    private const int AnnoProprio = 2029;
    private const int MeseProprio = 9;

    private string _cn = null!;
    private EmailRelService _service = null!;

    [SetUp]
    public void Setup()
    {
        _cn = LocalTestDb.ConnectionString;
        TestDb.SkipIfUnavailable(_cn);
        _service = new EmailRelService(_cn);
        PulisciPeriodoProprio();
    }

    [TearDown]
    public void TearDown()
    {
        if (_cn is not null)
            PulisciPeriodoProprio();
    }

    [Test]
    public void GetSenderEmail_PeriodoSeedato_ShouldTrovareIlDestinatarioConLaPecRisolta()
    {
        var destinatari = _service.GetSenderEmail(AnnoSeed, MeseSeed, TipologiaSeed, "REL")?.ToList();

        Assert.That(destinatari, Is.Not.Null.And.Not.Empty,
            "Nessun destinatario per il periodo seedato: se la query e' corretta, controllare che "
            + "views/93_EmailRel.sql e email_rel.sql siano stati applicati al container.");

        var ente = destinatari!.Single(x => x.IdEnte == EnteSeedPac);

        Assert.Multiple(() =>
        {
            Assert.That(ente.Pec, Is.EqualTo("ente1@pec.invalid"),
                "PEC risolta dalla sorgente sbagliata: la vista usa una cascata "
                + "ISNULL(DatiFatturazione.PEC, RelPecEmail.pec, Enti.digitalAddress).");
            Assert.That(ente.TipoContratto, Is.EqualTo("PAC"));
            Assert.That(ente.TipologiaFattura, Is.EqualTo(TipologiaSeed));
            Assert.That(ente.Anno, Is.EqualTo(AnnoSeed));
            Assert.That(ente.Mese, Is.EqualTo(MeseSeed));
        });
    }

    /// <summary>
    /// Il test piu' importante della classe. Sullo stesso periodo esistono due REL identiche, una di
    /// un ente PAC e una di un ente PAL: deve tornare solo la prima. Costruire entrambe le righe e'
    /// cio' che rende la verifica reale — con il solo ente PAC il test passerebbe anche se il filtro
    /// venisse rimosso.
    /// </summary>
    [Test]
    public void GetSenderEmail_EntePal_ShouldEssereEsclusoDalPac()
    {
        CreaRelTestata(EnteSeedPac, "TOKEN-E1", 300.00m);
        CreaRelTestata(EntePal, "TOKEN-E3", 300.00m);

        var destinatari = _service.GetSenderEmail(AnnoProprio, MeseProprio, TipologiaSeed, "REL")?.ToList();

        Assert.That(destinatari, Is.Not.Null.And.Not.Empty, "Nemmeno l'ente PAC e' stato trovato.");

        Assert.Multiple(() =>
        {
            Assert.That(destinatari!.Select(x => x.IdEnte), Does.Contain(EnteSeedPac));
            Assert.That(destinatari!.Select(x => x.IdEnte), Does.Not.Contain(EntePal),
                "Un ente PAL e' finito fra i destinatari della PEC di Regolare Esecuzione: la query "
                + "deve filtrare tp.Descrizione = 'PAC'.");
        });
    }

    /// <summary>
    /// La query pretende Totale > 0: una REL a zero non produce comunicazione.
    /// </summary>
    [Test]
    public void GetSenderEmail_RelConTotaleZero_ShouldEssereEsclusa()
    {
        CreaRelTestata(EnteSeedPac, "TOKEN-E1", 0.00m);

        var destinatari = _service.GetSenderEmail(AnnoProprio, MeseProprio, TipologiaSeed, "REL")?.ToList();

        Assert.That(destinatari, Is.Not.Null.And.Empty,
            "Una REL con Totale = 0 non deve generare un destinatario.");
    }

    [Test]
    public void GetSenderEmail_SenzaTipoComunicazione_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () => _service.GetSenderEmail(AnnoSeed, MeseSeed, TipologiaSeed, null));
    }

    /// <summary>
    /// La garanzia su cui si regge preview=true: l'anteprima finisce nella tabella di staging con
    /// Invio = 0 e la tabella degli invii reali resta intatta. Se un domani InsertPreviewEmail
    /// scrivesse (anche) su pfd.RelEmail, un giro di prova sembrerebbe un invio avvenuto.
    /// </summary>
    [Test]
    public void InsertPreviewEmail_ShouldScrivereSoloNelloStaging_ConInvioZero()
    {
        var trackingPrima = ContaRighe("pfd.RelEmail");

        var esito = _service.InsertPreviewEmail(TrackingDiProva(invio: 0));

        Assert.That(esito, Is.True, "InsertPreviewEmail ha restituito false.");
        Assert.Multiple(() =>
        {
            Assert.That(ContaRighe("stg.RelEmailPreview"), Is.EqualTo(1),
                "L'anteprima non e' stata scritta in stg.RelEmailPreview.");
            Assert.That(LeggiInvio("stg.RelEmailPreview"), Is.EqualTo(0),
                "L'anteprima deve avere Invio = 0: e' il flag che distingue una prova da un invio reale.");
            Assert.That(ContaRighe("pfd.RelEmail"), Is.EqualTo(trackingPrima),
                "Un'anteprima ha scritto nella tabella degli invii reali.");
        });
    }

    [Test]
    public void InsertTracciatoEmail_ShouldScrivereNelTracking()
    {
        var esito = _service.InsertTracciatoEmail(TrackingDiProva(invio: 1));

        Assert.That(esito, Is.True, "InsertTracciatoEmail ha restituito false.");
        Assert.That(ContaRighe("pfd.RelEmail"), Is.EqualTo(1));
    }

    /// <summary>
    /// Caratterizzazione di un difetto reale, trovato scrivendo questi test (15/09/2026).
    ///
    /// InsertPreviewEmail passa i parametri con `.Value = email.Sospesa` senza convertire il null in
    /// DBNull.Value: con Sospesa, Multipla o TipoContratto a null, ADO.NET considera il parametro non
    /// fornito e l'esecuzione fallisce. L'eccezione finisce in un `catch` vuoto (che il codice stesso
    /// segnala con un commento) e il metodo restituisce semplicemente **false**.
    ///
    /// Perche' conta: nessun chiamante controlla quel bool. L'anteprima non verrebbe scritta, il log
    /// direbbe comunque "Modalita' preview: email NON inviata", e chi verifica un giro di prova
    /// guardando stg.RelEmailPreview troverebbe la tabella vuota senza alcun errore da nessuna parte.
    ///
    /// Oggi non si manifesta perche' SendEmail valorizza sempre quei campi. Questo test fissa il
    /// comportamento attuale: se un domani il null venisse gestito (DBNull.Value), diventa rosso —
    /// ed e' il segnale per aggiornarlo, non un test da riparare.
    /// </summary>
    [Test]
    public void InsertPreviewEmail_ConCampiOpzionaliNull_ShouldFallireInSilenzio_Caratterizzazione()
    {
        var tracking = TrackingDiProva(invio: 0);
        tracking.Sospesa = null;
        tracking.Multipla = null;
        tracking.TipoContratto = null;

        var esito = _service.InsertPreviewEmail(tracking);

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.False,
                "Se ora restituisce true, il null viene gestito: difetto corretto, aggiornare il test.");
            Assert.That(ContaRighe("stg.RelEmailPreview"), Is.Zero,
                "Nessuna riga scritta, e nessuna eccezione propagata: e' esattamente il punto.");
        });
    }

    // --- helper --------------------------------------------------------------------------------

    /// <summary>
    /// Ricalca com'e' costruito il tracking nel flusso reale (SendEmail.cs, righe 195-213).
    /// ⚠️ Sospesa, Multipla e TipoContratto vanno valorizzati anche quando non significano nulla:
    /// li' il codice mette (byte)0, non null. Lasciarli null fa fallire la scrittura in silenzio —
    /// v. il test di caratterizzazione sotto.
    /// </summary>
    private RelEmailTracking TrackingDiProva(byte invio) => new()
    {
        IdEnte = EnteSeedPac,
        IdContratto = "TOKEN-E1",
        TipologiaFattura = TipologiaSeed,
        Anno = AnnoProprio,
        Mese = MeseProprio,
        Data = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
        Pec = "ente1@pec.invalid",
        Messaggio = "messaggio di prova",
        Oggetto = "oggetto di prova",
        Corpo = "<p>corpo di prova</p>",
        RagioneSociale = "Ente Test 1",
        Invio = invio,
        TipoComunicazione = "REL",
        Sospesa = 0,
        Multipla = 0,
        TipoContratto = "PAC",
        Fase = null
    };

    private void CreaRelTestata(string idEnte, string contratto, decimal totale) => Esegui(
        @"INSERT INTO pfd.RelTestata
            (internal_organization_id, contract_id, TipologiaFattura, [year], [month],
             TotaleAnalogico, TotaleDigitale, TotaleNotificheAnalogiche, TotaleNotificheDigitali,
             Totale, TotaleAnalogicoIva, TotaleDigitaleIva, TotaleIva, Caricata, RelFatturata,
             AsseverazioneTotaleAnalogico, AsseverazioneTotaleDigitale,
             AsseverazioneTotaleNotificheAnalogiche, AsseverazioneTotaleNotificheDigitali,
             AsseverazioneTotale, AsseverazioneTotaleAnalogicoIva, AsseverazioneTotaleDigitaleIva,
             AsseverazioneTotaleIva)
          VALUES (@ente, @contratto, @tipologia, @anno, @mese,
                  @totale, 0, 1, 0, @totale, 0, 0, 0, 0, 0,
                  0, 0, 0, 0, 0, 0, 0, 0);",
        // Le colonne Asseverazione* sono valorizzate di proposito: sul DTO di lettura del dettaglio
        // REL sono decimal/int non nullable, e un NULL fa fallire il mapping Dapper con un errore
        // che sembra un problema di vista e non lo e'.
        cmd =>
        {
            cmd.Parameters.AddWithValue("@ente", idEnte);
            cmd.Parameters.AddWithValue("@contratto", contratto);
            cmd.Parameters.AddWithValue("@tipologia", TipologiaSeed);
            cmd.Parameters.AddWithValue("@anno", AnnoProprio);
            cmd.Parameters.AddWithValue("@mese", MeseProprio);
            cmd.Parameters.AddWithValue("@totale", totale);
        });

    private void PulisciPeriodoProprio()
    {
        foreach (var tabella in new[] { "pfd.RelTestata", "pfd.RelEmail", "stg.RelEmailPreview" })
        {
            var colonnaAnno = tabella == "pfd.RelTestata" ? "[year]" : "[year]";
            Esegui($"DELETE FROM {tabella} WHERE {colonnaAnno} = @anno AND [month] = @mese;", cmd =>
            {
                cmd.Parameters.AddWithValue("@anno", AnnoProprio);
                cmd.Parameters.AddWithValue("@mese", MeseProprio);
            });
        }
    }

    private int ContaRighe(string tabella) => Scalare(
        $"SELECT COUNT(*) FROM {tabella} WHERE [year] = @anno AND [month] = @mese;");

    private int LeggiInvio(string tabella) => Scalare(
        $"SELECT TOP 1 CAST(Invio AS int) FROM {tabella} WHERE [year] = @anno AND [month] = @mese;");

    private int Scalare(string sql)
    {
        using var conn = new SqlConnection(_cn);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@anno", AnnoProprio);
        cmd.Parameters.AddWithValue("@mese", MeseProprio);
        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
    }

    private void Esegui(string sql, Action<SqlCommand> parametri)
    {
        using var conn = new SqlConnection(_cn);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        parametri(cmd);
        cmd.ExecuteNonQuery();
    }
}
