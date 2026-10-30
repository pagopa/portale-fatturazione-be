using System.Data;
using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// PF-959: lo sheet "Regolari Esecuzioni" del report di saldo deve mostrare anche le REL degli enti
/// **PAC** non ancora fatturate, **firmate o no**. Prima non comparivano: `_sqlRel` parte da
/// `pfd.FattureTestata`, e finche' la REL non e' firmata la fattura non viene emessa. La soluzione e'
/// una UNION in coda (`FattureRelExcelBuilder.SelectRelPacNonFatturate`) che legge `pfd.RelTestata`.
///
/// Tre gruppi di test:
/// - **funzionali**: la REL compare, con le colonne Rel* e senza fattura, anche se l'ente l'ha solo
///   scaricata (riga DOWN nel log: e' il caso per cui il ramo non usa `pfd.RelUpload`);
/// - **adversarial**: cio' che deve restare fuori (PAL, fatturate, a zero, altro periodo, contratto
///   sconosciuto, staging, fattura gia' emessa) e i filtri opzionali, anche con valori ostili;
/// - **regressione**: la riga della fattura emessa, gli storni, lo sheet "Enti Fatt." e l'errore su
///   fattura senza righe restano quelli di prima.
///
/// Periodo dedicato 2093/4, tutto in una transazione annullata nel TearDown: nessuna riga resta nel
/// DB seedato. Fa eccezione l'ultimo test, che legge in sola lettura il seed del 2026/2.
/// </summary>
[NonParallelizable]
public class FattureRelExcelPacNonFatturateIntegrationTests
{
    private const int Anno = 2093;
    private const int Mese = 4;
    private const string Tipologia = "PRIMO SALDO";

    private const string EntePac = "11111111-1111-1111-1111-111111111111";   // contratto PAC, TOKEN-E1
    private const string ContrattoPac = "TOKEN-E1";
    private const string EntePal = "33333333-3333-3333-3333-333333333333";   // contratto PAL, TOKEN-E3
    private const string ContrattoPal = "TOKEN-E3";
    private const string SecondoContrattoPac = "TOKEN-E1-PF959";

    private SqlConnection _connection = null!;
    private SqlTransaction _transaction = null!;

    [SetUp]
    public async Task Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "pfd.RelUpload");
        _connection = new SqlConnection(LocalTestDb.ConnectionString);
        await _connection.OpenAsync();
        _transaction = _connection.BeginTransaction();
    }

    [TearDown]
    public void Cleanup()
    {
        try
        {
            _transaction?.Rollback();
        }
        finally
        {
            _transaction?.Dispose();
            _connection?.Dispose();
        }
    }

    // --- funzionali ------------------------------------------------------------------------------

    [Test]
    public async Task PacNonFirmata_SenzaFattura_CompareConLeColonneRel()
    {
        await Rel(EntePac, ContrattoPac, caricata: 0, totale: 100m);

        var riga = (await Leggi()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(riga.IdEnte, Is.EqualTo(EntePac));
            Assert.That(riga.IdContratto, Is.EqualTo(ContrattoPac));
            Assert.That(riga.RagioneSociale, Is.EqualTo("Ente Test 1"));
            Assert.That(riga.TipologiaFattura, Is.EqualTo(Tipologia));
            Assert.That((riga.Anno, riga.Mese), Is.EqualTo((Anno, Mese)));
            Assert.That(riga.TipologiaContratto, Is.EqualTo("PAC"));
            Assert.That(riga.IdFattura, Is.Null);
            Assert.That(riga.Progressivo, Is.Null);
            Assert.That(riga.TipoDocumento, Is.Null);
            Assert.That(riga.CodiceMateriale, Is.Null);
            Assert.That(riga.TotaleFatturaImponibile, Is.EqualTo(0m));
            Assert.That(riga.RelTotaleAnalogico, Is.EqualTo(40m));
            Assert.That(riga.RelTotaleDigitale, Is.EqualTo(60m));
            Assert.That(riga.RelTotaleNotificheAnalogiche, Is.EqualTo(4));
            Assert.That(riga.RelTotaleNotificheDigitali, Is.EqualTo(6));
            Assert.That(riga.RelTotaleNotifiche, Is.EqualTo(10));
            Assert.That(riga.RelTotale, Is.EqualTo(100m));
            Assert.That(riga.RelTotaleIvato, Is.EqualTo(122m));
            Assert.That(riga.Caricata, Is.EqualTo(0));
            Assert.That(riga.Firmata, Is.EqualTo("Non Caricata"));
            Assert.That(new[] { riga.StornoAnticipoAnalogico, riga.StornoAnticipoDigitale,
                                riga.StornoAccontoAnalogico, riga.StornoAccontoDigitale }, Is.All.EqualTo(0m));
        });
    }

    /// <summary>
    /// Firmata ma non ancora fatturata (es. fattura sospesa sotto soglia). A differenza di
    /// `RelNonFatturateSQLBuilder`, qui non serve un upload nel log: lo stato viene da `Caricata`.
    /// </summary>
    [Test]
    public async Task PacFirmata_NonFatturata_CompareAncheSenzaUploadNelLog()
    {
        await Rel(EntePac, ContrattoPac, caricata: 1);

        var riga = (await Leggi()).Single();

        Assert.That(riga.Caricata, Is.EqualTo(1));
        Assert.That(riga.Firmata, Is.EqualTo("Firmata"));
    }

    /// <summary>Il caso del requisito: l'ente ha scaricato la REL per firmarla, e non l'ha ancora caricata.</summary>
    [Test]
    public async Task PacScaricataNonFirmata_CompareUnaVolta()
    {
        await Rel(EntePac, ContrattoPac, caricata: 0);
        await Log(EntePac, ContrattoPac, "DOWN", new DateTime(2093, 5, 2));
        await Log(EntePac, ContrattoPac, "DOWN", new DateTime(2093, 5, 3));

        var righe = await Leggi();

        Assert.That(righe, Has.Count.EqualTo(1));
        Assert.That(righe[0].Firmata, Is.EqualTo("Non Caricata"));
    }

    [Test]
    public async Task PacFirmata_ConPiuUploadEDownload_UnaSolaRiga()
    {
        await Rel(EntePac, ContrattoPac, caricata: 1);
        await Log(EntePac, ContrattoPac, "UP", new DateTime(2093, 5, 2));
        await Log(EntePac, ContrattoPac, "DOWN", new DateTime(2093, 5, 3));
        await Log(EntePac, ContrattoPac, "UP", new DateTime(2093, 5, 4));

        Assert.That(await Leggi(), Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Stesso ente: fattura emessa su un contratto, REL non fatturata su un altro. La chiave C# della
    /// riga non fatturata include il contratto, altrimenti la fattura dello stesso ente la nasconderebbe.
    /// </summary>
    [Test]
    public async Task StessoEnte_FatturaSuUnContratto_RelNonFatturataSullAltro_DueRighe()
    {
        await Contratto(EntePac, SecondoContrattoPac, tipoContratto: 2);
        var idFattura = await FatturaConRel(EntePac, ContrattoPac, 300m);
        await Rel(EntePac, SecondoContrattoPac, caricata: 0);

        var righe = await Leggi();

        Assert.That(righe.Select(r => (r.IdFattura, r.IdContratto)), Is.EquivalentTo(new[]
        {
            (idFattura.ToString(), ContrattoPac),
            ((string?)null, SecondoContrattoPac)
        }));
    }

    // --- adversarial -----------------------------------------------------------------------------

    [TestCase("PAL")]
    [TestCase("gia' fatturata")]
    [TestCase("totale zero")]
    [TestCase("totale negativo")]
    [TestCase("altro mese")]
    [TestCase("altra tipologia")]
    [TestCase("contratto sconosciuto")]
    public async Task RelChe_NonEUnaPacDaFatturare_RestaFuori(string caso)
    {
        switch (caso)
        {
            case "PAL": await Rel(EntePal, ContrattoPal, caricata: 0); break;
            case "gia' fatturata": await Rel(EntePac, ContrattoPac, caricata: 1, relFatturata: 1); break;
            case "totale zero": await Rel(EntePac, ContrattoPac, caricata: 0, totale: 0m); break;
            case "totale negativo": await Rel(EntePac, ContrattoPac, caricata: 0, totale: -10m); break;
            case "altro mese": await Rel(EntePac, ContrattoPac, caricata: 0, mese: Mese + 1); break;
            case "altra tipologia": await Rel(EntePac, ContrattoPac, caricata: 0, tipologia: "SECONDO SALDO"); break;
            default: await Rel(EntePac, "TOKEN-INESISTENTE", caricata: 0); break;
        }

        Assert.That(await Leggi(), Is.Empty);
    }

    /// <summary>
    /// `RelFatturata` disallineato: la fattura e' gia' emessa ma la REL risulta ancora non fatturata.
    /// Deve uscire una riga sola, quella della fattura, non anche quella "non fatturata".
    /// </summary>
    [Test]
    public async Task FatturaGiaEmessa_ConRelFatturataAZero_UnaSolaRigaConLaFattura()
    {
        var idFattura = await FatturaConRel(EntePac, ContrattoPac, 300m, relFatturata: 0);

        var righe = await Leggi();

        Assert.That(righe.Select(r => r.IdFattura), Is.EqualTo(new[] { idFattura.ToString() }));
    }

    /// <summary>Stessa regola dello staging del ramo delle fatture: fuori POSTICIPATA (0) ed ELIMINATA (3).</summary>
    [TestCase(0, false)]
    [TestCase(3, false)]
    [TestCase(1, true)]
    [TestCase(2, true)]
    public async Task Staging_EscludePosticipateEdEliminate(int stato, bool presente)
    {
        await Rel(EntePac, ContrattoPac, caricata: 0);
        await Staging(EntePac, stato);

        Assert.That(await Leggi(), Has.Count.EqualTo(presente ? 1 : 0));
    }

    /// <summary>
    /// La REL non fatturata non ha una fattura, quindi non e' inviata: compare con "Tutte" e con "Non
    /// inviate". Il filtro sulle fatture emesse resta quello di prima (qui una fattura inviata).
    /// </summary>
    [TestCase(null, true, true)]
    [TestCase(0, true, false)]
    [TestCase(1, false, true)]
    [TestCase(2, false, false)]
    public async Task FiltroFatturaInviata_SuEntrambiIRami(int? inviata, bool relAttesa, bool fatturaAttesa)
    {
        await Rel(EntePac, ContrattoPac, caricata: 0);
        await FatturaConRel(EntePal, ContrattoPal, 200m, inviata: true);
        var query = Query();
        query.FatturaInviata = inviata;

        var righe = await Leggi(query);

        Assert.Multiple(() =>
        {
            Assert.That(righe.Any(r => r.IdEnte == EntePac && r.IdFattura is null), Is.EqualTo(relAttesa));
            Assert.That(righe.Any(r => r.IdEnte == EntePal && r.IdFattura is not null), Is.EqualTo(fatturaAttesa));
        });
    }

    [TestCase("nessuno", 2)]
    [TestCase("solo pac", 1)]
    [TestCase("solo pal", 1)]
    [TestCase("duplicato", 1)]
    [TestCase("inesistente", 0)]
    [TestCase("ostile", 0)]
    public async Task FiltroEnti_ApplicatoAEntrambiIRami(string caso, int attese)
    {
        await Rel(EntePac, ContrattoPac, caricata: 0);
        await FatturaConRel(EntePal, ContrattoPal, 200m);
        var query = Query();
        query.IdEnti = caso switch
        {
            "nessuno" => Array.Empty<string>(),
            "solo pac" => new[] { EntePac },
            "solo pal" => new[] { EntePal },
            "duplicato" => new[] { EntePac, EntePac },
            "inesistente" => new[] { "ente-inesistente" },
            _ => new[] { "' OR 1=1 --" }
        };

        var righe = await Leggi(query);

        Assert.That(righe, Has.Count.EqualTo(attese));
        if (caso is "solo pac" or "duplicato")
            Assert.That(righe[0].IdFattura, Is.Null);
        if (caso == "solo pal")
            Assert.That(righe[0].IdFattura, Is.Not.Null);
    }

    [TestCase(null, 2)]
    [TestCase(2, 1)]
    [TestCase(1, 1)]
    [TestCase(99, 0)]
    public async Task FiltroTipoContratto_ApplicatoAEntrambiIRami(int? tipoContratto, int attese)
    {
        await Rel(EntePac, ContrattoPac, caricata: 0);
        await FatturaConRel(EntePal, ContrattoPal, 200m);
        var query = Query();
        query.FkIdTipoContratto = tipoContratto;

        var righe = await Leggi(query);

        Assert.That(righe, Has.Count.EqualTo(attese));
        if (tipoContratto == 2)
            Assert.That(righe[0].IdEnte, Is.EqualTo(EntePac));
        if (tipoContratto == 1)
            Assert.That(righe[0].IdEnte, Is.EqualTo(EntePal));
    }

    // --- regressione -----------------------------------------------------------------------------

    /// <summary>La riga della fattura emessa, storni compresi, e' la stessa con o senza REL non fatturate.</summary>
    [Test]
    public async Task FatturaEmessaConStorni_InvariataConRelNonFatturatePresenti()
    {
        var idFattura = await FatturaConRel(EntePal, ContrattoPal, 250m, materiale: "MATERIALE");
        await Riga(idFattura, "STORNO ANT. NA", 10m);
        await Riga(idFattura, "STORNO ANTICIPO ND", 20m);
        await Riga(idFattura, "STORNO ACCONTO NA", 30m);
        await Riga(idFattura, "STORNO ACCONTO ND", 40m);
        var prima = (await Leggi()).Single();

        await Rel(EntePac, ContrattoPac, caricata: 0);
        var dopo = (await Leggi()).Single(r => r.IdFattura is not null);

        Assert.Multiple(() =>
        {
            Assert.That(prima.StornoAnticipoAnalogico, Is.EqualTo(-10m));
            Assert.That(prima.StornoAnticipoDigitale, Is.EqualTo(-20m));
            Assert.That(prima.StornoAccontoAnalogico, Is.EqualTo(-30m));
            Assert.That(prima.StornoAccontoDigitale, Is.EqualTo(-40m));
            Assert.That(dopo.IdFattura, Is.EqualTo(prima.IdFattura));
            Assert.That(dopo.TotaleFatturaImponibile, Is.EqualTo(prima.TotaleFatturaImponibile));
            Assert.That(dopo.RelTotale, Is.EqualTo(prima.RelTotale));
            Assert.That((dopo.StornoAnticipoAnalogico, dopo.StornoAnticipoDigitale, dopo.StornoAccontoAnalogico, dopo.StornoAccontoDigitale),
                Is.EqualTo((prima.StornoAnticipoAnalogico, prima.StornoAnticipoDigitale, prima.StornoAccontoAnalogico, prima.StornoAccontoDigitale)));
        });
    }

    /// <summary>Lo sheet "Enti Fatt." (seconda lista) usa `SelectRel` ma non la UNION: le REL non fatturate restano fuori.</summary>
    [Test]
    public async Task SheetEntiFatt_NonContieneLeRelNonFatturate()
    {
        await Rel(EntePac, ContrattoPac, caricata: 0);
        var idFattura = await FatturaConRel(EntePal, ContrattoPal, 200m);

        var righe = (await new FattureUnionRelExcelPersistence(Query()).Execute(_connection, "pfd", _transaction))!.ToList();

        Assert.That(righe.Select(r => r.IdFattura), Is.EqualTo(new[] { idFattura.ToString() }));
    }

    [Test]
    public async Task FatturaSenzaRighe_SollevaAncoraDataException()
    {
        await Rel(EntePac, ContrattoPac, caricata: 0);
        var idFattura = await FatturaConRel(EntePal, ContrattoPal, 200m, materiale: null);

        var errore = Assert.ThrowsAsync<DataException>(() => Leggi());
        Assert.That(errore!.Message, Does.Contain("CodiceMateriale NULL").And.Contain(idFattura.ToString()));
    }

    /// <summary>
    /// Seed reale (2026/2/SECONDO SALDO, v. `tests/Data/gestione_fatture.sql`): la REL di ente1 ha gia'
    /// la fattura 8001, quindi nella prima lista c'e' la fattura e non una seconda riga "non fatturata".
    /// Sola lettura, fuori dalla transazione: passa dall'handler come il report vero.
    /// </summary>
    [Test]
    public async Task Seed_RelConFatturaEmessa_NonRaddoppiaNellaPrimaLista()
    {
        var handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
        var liste = await handler.Send(new FattureRelExcelQuery(new AuthenticationInfo { Prodotto = "prod-pn", Ruolo = Ruolo.ADMIN })
        {
            Anno = 2026, Mese = 2, TipologiaFattura = "SECONDO SALDO"
        });

        var ente1 = liste![0].Where(r => r.IdEnte == EntePac).ToList();
        Assert.That(ente1.Select(r => r.IdFattura), Is.EqualTo(new[] { "8001" }));
    }

    // ---------------------------------------------------------------------------------------------

    private static FattureRelExcelQuery Query() => new(new AuthenticationInfo())
    {
        Anno = Anno, Mese = Mese, TipologiaFattura = Tipologia
    };

    private async Task<List<FattureRelExcelDto>> Leggi(FattureRelExcelQuery? query = null) =>
        (await new FattureRelExcelBuilderPersistence(query ?? Query()).Execute(_connection, "pfd", _transaction))!.ToList();

    private Task Rel(string ente, string contratto, byte caricata, byte relFatturata = 0, decimal totale = 100m,
        int mese = Mese, string tipologia = Tipologia) =>
        _connection.ExecuteAsync("""
            INSERT INTO pfd.RelTestata
                (internal_organization_id, contract_id, TipologiaFattura, [year], [month], TotaleAnalogico, TotaleDigitale,
                 TotaleNotificheAnalogiche, TotaleNotificheDigitali, Totale, TotaleAnalogicoIva, TotaleDigitaleIva, TotaleIva,
                 Caricata, RelFatturata)
            VALUES (@ente, @contratto, @tipologia, @anno, @mese, @totale * 0.4, @totale * 0.6,
                    4, 6, @totale, @totale * 0.488, @totale * 0.732, @totale * 1.22, @caricata, @relFatturata)
            """, new { ente, contratto, tipologia, anno = Anno, mese, totale, caricata, relFatturata }, _transaction);

    /// <summary>Fattura emessa con la sua REL (il ramo delle fatture esige la REL corrispondente) e una riga.</summary>
    private async Task<long> FatturaConRel(string ente, string contratto, decimal totale, bool? inviata = false,
        byte relFatturata = 1, string? materiale = "MATERIALE")
    {
        await Rel(ente, contratto, caricata: 1, relFatturata: relFatturata, totale: totale);
        var id = await _connection.QuerySingleAsync<long>("""
            INSERT INTO pfd.FattureTestata
                (FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura, IdentificativoFattura,
                 TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento, MeseRiferimento, CodiceContratto,
                 Progressivo, FatturaInviata)
            OUTPUT INSERTED.IdFattura
            VALUES ('prod-pn', 'TD01', @tipologia, @ente, '2093-05-01', 'PF959', @totale, 'EUR', 'MP5', @anno, @mese,
                    @contratto, 959, @inviata)
            """, new { ente, contratto, totale, inviata, tipologia = Tipologia, anno = Anno, mese = Mese }, _transaction);
        if (materiale is not null)
            await Riga(id, materiale, totale);
        return id;
    }

    private Task Riga(long id, string materiale, decimal importo) =>
        _connection.ExecuteAsync("""
            INSERT INTO pfd.FattureRighe
                (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
            VALUES (@id, 1, 'PF-959', @materiale, 1, @importo, @importo, 0, '04/2093')
            """, new { id, materiale, importo }, _transaction);

    private Task Log(string ente, string contratto, string azione, DateTime data) =>
        _connection.ExecuteAsync("""
            INSERT INTO pfd.RelUpload (FkIdEnte, contract_id, TipologiaFattura, [year], [month], DataEvento, IdUtente, Azione, Hash)
            VALUES (@ente, @contratto, @tipologia, @anno, @mese, @data, 'integration-test-pf959', @azione, 'hash')
            """, new { ente, contratto, tipologia = Tipologia, anno = Anno, mese = Mese, data, azione }, _transaction);

    private Task Contratto(string ente, string token, int tipoContratto) =>
        _connection.ExecuteAsync("""
            INSERT INTO pfd.Contratti (internalistitutionid, FkIdTipoContratto, onboardingtokenid)
            VALUES (@ente, @tipoContratto, @token)
            """, new { ente, token, tipoContratto }, _transaction);

    private Task Staging(string ente, int stato) =>
        _connection.ExecuteAsync("""
            INSERT INTO cfg.GestioneFatture
                (FkIdEnte, FkTipologiaFattura, Anno, Mese, Stato, Azione, IdUtenteInserimento, DataInserimento)
            VALUES (@ente, @tipologia, @anno, @mese, @stato, @azione, 'integration-test-pf959', GETDATE())
            """, new
            {
                ente, stato, tipologia = Tipologia, anno = Anno, mese = Mese,
                azione = stato switch { 0 => "POSTICIPATA", 1 => "RIPRISTINATA", 2 => "CANCELLATA", _ => "ELIMINATA" }
            }, _transaction);
}
