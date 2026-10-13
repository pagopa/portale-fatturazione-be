using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// `RelNonFatturateQuery` alimenta lo sheet "Reg. Esec. PAC non firmate" di `api/fatture/report` e
/// del report non inviate, e l'Excel di `api/rel/pagopa/nonfatturate`.
///
/// Nata dalla segnalazione del 12/10/2026 (report PRIMO SALDO 2026/8: INPS assente dallo sheet ma "in
/// attesa di firma" in "Enti Fatt. AGOSTO Sospesi"). Quel caso NON dipende da questa query: la REL di
/// INPS era firmata, e a sbagliare e' `be.vwFattureSospeseReport`, che legge `Caricata` dallo staging.
///
/// Analizzandola sono pero' emersi due difetti della query, non corretti (non verificabili ora sui
/// dati reali) e documentati dai test `[Ignore]`: la query fa `LEFT JOIN pfd.RelUpload`, che e' un
/// LOG (una riga per ogni upload E download), e filtra `(Azione = 'UP' AND Caricata = 1) OR
/// (Azione IS NULL AND Caricata = 0)`. Una REL non firmata ma **scaricata** produce la riga
/// `DOWN`/`Caricata = 0`, che non soddisfa nessuno dei due rami e sparisce; piu' upload, o piu'
/// contratti dello stesso ente, la duplicano.
///
/// Dal 13/10/2026 `ReportFattureRel` non scarta piu' le REL firmate (`Caricata != 1` commentato): lo
/// sheet mostra le PAC non fatturate firmate e no. Quindi e' questa query a decidere quali firmate
/// entrano (solo quelle con un upload nel log), e la duplicazione per piu' upload, prima nascosta dal
/// filtro C#, ora arriva nello sheet.
///
/// Periodo dedicato 2031/3, creato in SetUp e distrutto in TearDown: una `pfd.RelTestata` in piu' su
/// un periodo condiviso cambia l'esito di altre fixture (v. § periodi riservati in
/// `docs/test-integrazione-db-seedato.md`).
/// </summary>
public class RelNonFatturateQueryIntegrationTests
{
    private const string Ente = "11111111-1111-1111-1111-111111111111"; // ente1, contratto PAC
    private const string Contratto = "TOKEN-E1";
    private const string SecondoContratto = "TOKEN-E1-RELNF";
    private const int Anno = 2031;
    private const int Mese = 3;

    private string _cn = null!;
    private IMediator _handler = null!;
    private bool _seedPronto;

    [SetUp]
    public void Setup()
    {
        _seedPronto = false;
        _cn = LocalTestDb.ConnectionString;
        TestDb.SkipIfUnavailable(_cn);
        TestDb.SkipSeOggettoAssente(_cn, "pfd.RelUpload");
        _handler = ServiceProvider.GetRequiredService<IMediator>(_cn);
        Pulisci();
        _seedPronto = true;
    }

    [TearDown]
    public void TearDown()
    {
        if (_seedPronto)
            Pulisci();
    }

    /// <summary>Il caso della segnalazione: la REL e' stata scaricata, non firmata.</summary>
    [Test]
    [Ignore("Difetto noto, non corretto (12/10/2026): RelNonFatturateSQLBuilder fa LEFT JOIN pfd.RelUpload, che e' un log, " +
            "e filtra (Azione='UP' AND Caricata=1) OR (Azione IS NULL AND Caricata=0). Una REL non firmata ma scaricata " +
            "(riga DOWN) sparisce dallo sheet 'Reg. Esec. PAC non firmate'. Fix proposto: OUTER APPLY sull'ultimo UP " +
            "invece della join. Da riattivare quando la query viene corretta.")]
    public async Task RelPacNonFirmata_ScaricataDallEnte_ShouldRestareFraLeNonFirmate()
    {
        CreaTestata("PRIMO SALDO", caricata: 0);
        Log("PRIMO SALDO", "DOWN", new DateTime(2031, 4, 2));

        var rel = await Rel("PRIMO SALDO");

        Assert.That(rel, Has.Count.EqualTo(1), "un download non deve far sparire la REL non firmata");
        Assert.Multiple(() =>
        {
            Assert.That(rel[0].Caricata, Is.EqualTo(0));
            Assert.That(rel[0].TipoContratto, Is.EqualTo("PAC"));
            Assert.That(rel[0].Data, Is.Null, "la data e' quella dell'upload, e un download non lo e'");
        });
    }

    /// <summary>Controllo di base: senza alcun evento di log la REL non firmata c'era anche prima.</summary>
    [Test]
    public async Task RelPacNonFirmata_SenzaLog_ShouldComparire()
    {
        CreaTestata("PRIMO SALDO", caricata: 0);

        Assert.That(await Rel("PRIMO SALDO"), Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Una REL firmata e ricaricata due volte (piu' qualche download) e' UNA riga, con la data
    /// dell'ultimo upload. Con la join sul log usciva una riga per ogni upload.
    /// </summary>
    [Test]
    [Ignore("Difetto noto, non corretto (12/10/2026): la LEFT JOIN su pfd.RelUpload produce una riga per ogni upload, " +
            "quindi una REL ricaricata piu' volte esce duplicata. Fix proposto: OUTER APPLY con MAX(dataevento) sugli UP. " +
            "Da riattivare quando la query viene corretta.")]
    public async Task RelFirmata_ConPiuUploadEDownload_ShouldEssereUnaRigaConLUltimoUpload()
    {
        CreaTestata("PRIMO SALDO", caricata: 1);
        Log("PRIMO SALDO", "DOWN", new DateTime(2031, 4, 1));
        Log("PRIMO SALDO", "UP", new DateTime(2031, 4, 3));
        Log("PRIMO SALDO", "UP", new DateTime(2031, 4, 5));
        Log("PRIMO SALDO", "DOWN", new DateTime(2031, 4, 6));

        var rel = await Rel("PRIMO SALDO");

        Assert.That(rel, Has.Count.EqualTo(1));
        Assert.That(rel[0].Data, Is.EqualTo(new DateTime(2031, 4, 5)));
    }

    /// <summary>
    /// Un ente con due contratti: la join su `pfd.Contratti` era sul solo ente, quindi la REL usciva
    /// una volta per contratto. Ora si lega anche al contratto della REL.
    /// </summary>
    [Test]
    [Ignore("Difetto noto, non corretto (12/10/2026): la join su pfd.Contratti e' sul solo ente, senza " +
            "c.onboardingtokenid = r.contract_id, quindi un ente con piu' contratti duplica la REL. " +
            "Da riattivare quando la query viene corretta.")]
    public async Task EnteConDueContratti_ShouldRestituireLaRelUnaVolta()
    {
        AggiungiSecondoContratto();
        CreaTestata("PRIMO SALDO", caricata: 0);

        Assert.That(await Rel("PRIMO SALDO"), Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Il caso che il filtro C# tolto il 13/10/2026 rende visibile: una REL PAC firmata con il suo
    /// upload esce dalla query, con la data dell'upload, e quindi arriva nello sheet.
    /// </summary>
    [Test]
    public async Task RelPacFirmata_ConUpload_ShouldComparireConLaDataDellUpload()
    {
        CreaTestata("PRIMO SALDO", caricata: 1);
        Log("PRIMO SALDO", "UP", new DateTime(2031, 4, 3));

        var rel = await Rel("PRIMO SALDO");

        Assert.That(rel, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(rel[0].Caricata, Is.EqualTo(1));
            Assert.That(rel[0].Firmata, Is.EqualTo("Firmata"));
            Assert.That(rel[0].TipoContratto, Is.EqualTo("PAC"));
            Assert.That(rel[0].Data, Is.EqualTo(new DateTime(2031, 4, 3)));
        });
    }

    /// <summary>
    /// Caratterizzazione del difetto sospeso: una REL firmata e ricaricata due volte esce DUE volte,
    /// una per upload. Prima del 13/10/2026 il filtro C# `Caricata != 1` scartava le firmate e la
    /// duplicazione non arrivava nello sheet; ora si'. Quando la query verra' corretta questo test
    /// diventa rosso: va tolto e riattivato `RelFirmata_ConPiuUploadEDownload_...`.
    /// </summary>
    [Test]
    public async Task RelFirmata_ConDueUpload_EsceDuplicata_Caratterizzazione()
    {
        CreaTestata("PRIMO SALDO", caricata: 1);
        Log("PRIMO SALDO", "UP", new DateTime(2031, 4, 3));
        Log("PRIMO SALDO", "UP", new DateTime(2031, 4, 5));
        Log("PRIMO SALDO", "DOWN", new DateTime(2031, 4, 6));

        var rel = await Rel("PRIMO SALDO");

        Assert.That(rel.Select(x => x.Data),
            Is.EquivalentTo(new DateTime?[] { new DateTime(2031, 4, 3), new DateTime(2031, 4, 5) }),
            "una riga per ogni UP, il DOWN e' scartato dal filtro");
    }

    /// <summary>
    /// Una REL firmata (`Caricata = 1`) senza alcun upload nel log e' esclusa: con il filtro C# tolto,
    /// e' la query a decidere quali firmate entrano nello sheet. Un download non basta a includerla.
    /// </summary>
    [Test]
    public async Task RelFirmata_SenzaUpload_ShouldRestareEsclusa()
    {
        CreaTestata("PRIMO SALDO", caricata: 1);
        Log("PRIMO SALDO", "DOWN", new DateTime(2031, 4, 1));

        Assert.That(await Rel("PRIMO SALDO"), Is.Empty);
    }

    /// <summary>Le regole che restano: REL gia' fatturate e a totale zero non sono "non fatturate".</summary>
    [Test]
    public async Task RelFatturataOATotaleZero_ShouldEssereEsclusa()
    {
        CreaTestata("PRIMO SALDO", caricata: 0, relFatturata: 1);
        CreaTestata("SECONDO SALDO", caricata: 0, totale: 0m);

        var fatturata = await Rel("PRIMO SALDO");
        var aZero = await Rel("SECONDO SALDO");

        Assert.Multiple(() =>
        {
            Assert.That(fatturata, Is.Empty);
            Assert.That(aZero, Is.Empty);
        });
    }

    // ---------------------------------------------------------------------------------------------

    private async Task<List<RelNonFatturataDto>> Rel(string tipologia)
    {
        var tutte = await _handler.Send(new RelNonFatturateQuery(Auth()));
        return tutte!
            .Where(x => x.IdEnte == Ente && x.IdContratto == Contratto && x.Anno == Anno && x.Mese == Mese
                     && x.TipologiaFattura == tipologia)
            .ToList();
    }

    private void CreaTestata(string tipologia, byte caricata, byte relFatturata = 0, decimal totale = 100.00m) => Esegui(
        @"INSERT INTO pfd.RelTestata
            (internal_organization_id, contract_id, TipologiaFattura, [year], [month],
             TotaleAnalogico, TotaleDigitale, TotaleNotificheAnalogiche, TotaleNotificheDigitali,
             Totale, TotaleAnalogicoIva, TotaleDigitaleIva, TotaleIva, Caricata, RelFatturata,
             AsseverazioneTotaleAnalogico, AsseverazioneTotaleDigitale,
             AsseverazioneTotaleNotificheAnalogiche, AsseverazioneTotaleNotificheDigitali,
             AsseverazioneTotale, AsseverazioneTotaleAnalogicoIva, AsseverazioneTotaleDigitaleIva,
             AsseverazioneTotaleIva)
          VALUES (@ente, @contratto, @tipologia, @anno, @mese,
                  0, @totale, 0, 1, @totale, 0, @totale * 0.22, @totale * 0.22, @caricata, @relFatturata,
                  0, 0, 0, 0, 0, 0, 0, 0);",
        cmd =>
        {
            cmd.Parameters.AddWithValue("@ente", Ente);
            cmd.Parameters.AddWithValue("@contratto", Contratto);
            cmd.Parameters.AddWithValue("@tipologia", tipologia);
            cmd.Parameters.AddWithValue("@anno", Anno);
            cmd.Parameters.AddWithValue("@mese", Mese);
            cmd.Parameters.AddWithValue("@totale", totale);
            cmd.Parameters.AddWithValue("@caricata", caricata);
            cmd.Parameters.AddWithValue("@relFatturata", relFatturata);
        });

    private void Log(string tipologia, string azione, DateTime data) => Esegui(
        @"INSERT INTO pfd.RelUpload (FkIdEnte, contract_id, TipologiaFattura, [year], [month], DataEvento, IdUtente, Azione, Hash)
          VALUES (@ente, @contratto, @tipologia, @anno, @mese, @data, 'integration-test-relnf', @azione, 'hash');",
        cmd =>
        {
            cmd.Parameters.AddWithValue("@ente", Ente);
            cmd.Parameters.AddWithValue("@contratto", Contratto);
            cmd.Parameters.AddWithValue("@tipologia", tipologia);
            cmd.Parameters.AddWithValue("@anno", Anno);
            cmd.Parameters.AddWithValue("@mese", Mese);
            cmd.Parameters.AddWithValue("@data", data);
            cmd.Parameters.AddWithValue("@azione", azione);
        });

    // Copia del contratto esistente di ente1 con un altro token: stesso schema di ApiKeysQueryIntegrationTests.
    private void AggiungiSecondoContratto() => Esegui(
        @"INSERT INTO pfd.Contratti (InternalIstitutionId, onboardingtokenid, FkIdTipoContratto, product, codiceSDI)
          SELECT TOP 1 InternalIstitutionId, @token, FkIdTipoContratto, product, codiceSDI
            FROM pfd.Contratti WHERE InternalIstitutionId = @ente AND onboardingtokenid = @contratto;",
        cmd =>
        {
            cmd.Parameters.AddWithValue("@ente", Ente);
            cmd.Parameters.AddWithValue("@token", SecondoContratto);
            cmd.Parameters.AddWithValue("@contratto", Contratto);
        });

    private void Pulisci()
    {
        Esegui("DELETE FROM pfd.RelUpload WHERE FkIdEnte = @ente AND [year] = @anno AND [month] = @mese;", Periodo);
        Esegui("DELETE FROM pfd.RelTestata WHERE internal_organization_id = @ente AND [year] = @anno AND [month] = @mese;", Periodo);
        Esegui("DELETE FROM pfd.Contratti WHERE onboardingtokenid = @token;",
            cmd => cmd.Parameters.AddWithValue("@token", SecondoContratto));
    }

    private static void Periodo(SqlCommand cmd)
    {
        cmd.Parameters.AddWithValue("@ente", Ente);
        cmd.Parameters.AddWithValue("@anno", Anno);
        cmd.Parameters.AddWithValue("@mese", Mese);
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
        Id = "integration-test-relnf",
        IdEnte = Ente,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 2
    };
}
