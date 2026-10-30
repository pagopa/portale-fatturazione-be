using Dapper;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// PF-976 (PF-972): nel dettaglio di un documento contabile emesso (`_sqlSelectDettaglioEmesso`, rotte
/// `api/fatture/emesse/dettaglio` e `api/fatture/ente/emesse/dettaglio`) le sospese comparivano duplicate.
/// Le REL erano agganciate per ente, contratto, anno e mese, senza tipologia: con piu' REL nello stesso mese
/// ogni riga si moltiplicava. Le cause erano due e si sommavano:
/// - `pfd.RelTestata` del mese della fattura emessa (per esempio PRIMO + SECONDO SALDO);
/// - `pfd.tmpRelTestata` del mese di ciascuna sospesa.
///
/// Regola decisa col team DATA: la REL e' quella della **stessa tipologia**, e per ACCONTO e ANTICIPO e'
/// quella del **PRIMO SALDO**. Nessun ripiego: se nel mese manca, la testata resta senza REL (totali a 0).
///
/// Tre gruppi di test:
/// - **regressione**: il caso reale della fattura 120288 (10 righe invece di 4) e i totali della REL giusta;
/// - **funzionali**: la regola per tipologia, sulla REL definitiva e su quella temporanea;
/// - **adversarial**: REL di un altro periodo/contratto/ente, assenza di REL, parametri ostili.
///
/// Periodo dedicato 2094, ente1 del seed (PAC, TOKEN-E1), tutto in una transazione annullata nel TearDown.
/// </summary>
[NonParallelizable]
public class FattureDettaglioEmessoIntegrationTests
{
    private const int Anno = 2094;
    private const int Mese = 6;

    private const string Ente = "11111111-1111-1111-1111-111111111111";   // "Ente Test 1", PAC, TOKEN-E1
    private const string Contratto = "TOKEN-E1";
    private const string AltroEnte = "33333333-3333-3333-3333-333333333333";

    private const string PrimoSaldo = "PRIMO SALDO";
    private const string SecondoSaldo = "SECONDO SALDO";
    private const string SemSospesi = "SEM. SOSPESI";
    private const string VarSemestrale = "VAR. SEMESTRALE";

    private SqlConnection _connection = null!;
    private SqlTransaction _transaction = null!;

    [SetUp]
    public async Task Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "pfd.Iva");
        _connection = new SqlConnection(LocalTestDb.ConnectionString);
        await _connection.OpenAsync();
        _transaction = _connection.BeginTransaction();
        await Iva(Anno, Mese);
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

    // --- regressione -----------------------------------------------------------------------------

    /// <summary>
    /// Il caso della fattura 120288: PRIMO SALDO che consolida 4 sospese, due REL definitive nel mese
    /// (PRIMO + SECONDO SALDO) e una sospesa con due REL temporanee. Prima del fix: 10 righe (2 x 4, piu'
    /// 2 per la sospesa doppia). Con il solo filtro sulle temporanee: 8. Corretto: 4, una per sospesa.
    /// </summary>
    [Test]
    public async Task Caso120288_UnaRigaPerSospesa()
    {
        var id = await Emessa(PrimoSaldo);
        var sospese = new List<long>();
        foreach (var mese in new[] { 3, 4, 5, 6 })
            sospese.Add(await Sospesa(id, PrimoSaldo, mese));
        await Rel(PrimoSaldo, totale: 1000m);
        await Rel(SecondoSaldo, totale: 7777m);
        await RelTmp(PrimoSaldo, totale: 500m);
        await RelTmp(SecondoSaldo, totale: 9999m);

        var righe = await Leggi(id);

        Assert.Multiple(() =>
        {
            Assert.That(righe, Has.Count.EqualTo(4));
            Assert.That(righe.Select(r => r.IdFatturaSospesa), Is.EquivalentTo(sospese.Select(s => s.ToString())));
            Assert.That(righe.Select(r => r.RelTotale), Is.All.EqualTo(1000m));
            Assert.That(righe.Single(r => r.MeseSospesa == Mese).RelTotaleSospeso, Is.EqualTo(500m));
        });
    }

    /// <summary>
    /// Prima del fix la testata prendeva totali e "caricata" dalla prima riga, cioe' da una REL a caso:
    /// qui la REL del SECONDO SALDO e' caricata e quella del PRIMO SALDO no, e sono le uniche differenze.
    /// </summary>
    [Test]
    public async Task TestataPrimoSaldo_NonPrendeMaiCaricataETotaliDelSecondoSaldo()
    {
        var id = await Emessa(PrimoSaldo);
        await Sospesa(id, PrimoSaldo, Mese);
        await Rel(PrimoSaldo, totale: 1000m, caricata: 0);
        await Rel(SecondoSaldo, totale: 7777m, caricata: 1);

        var riga = (await Leggi(id)).Single();

        Assert.Multiple(() =>
        {
            Assert.That(riga.RelTotale, Is.EqualTo(1000m));
            Assert.That(riga.RelTotaleAnalogico, Is.EqualTo(400m));
            Assert.That(riga.RelTotaleNotifiche, Is.EqualTo(10));
            Assert.That(riga.Caricata, Is.False);
        });
    }

    [Test]
    public async Task UnaSolaRel_RisultatoInvariato()
    {
        var id = await Emessa(PrimoSaldo);
        await Sospesa(id, PrimoSaldo, Mese);
        await Rel(PrimoSaldo, totale: 1000m);
        await RelTmp(PrimoSaldo, totale: 500m);

        var riga = (await Leggi(id)).Single();

        Assert.Multiple(() =>
        {
            Assert.That(riga.IdFattura, Is.EqualTo(id));
            Assert.That(riga.RagioneSociale, Is.EqualTo("Ente Test 1"));
            Assert.That(riga.TipologiaContratto, Is.EqualTo("PAC"));
            Assert.That(riga.RelTotale, Is.EqualTo(1000m));
            Assert.That(riga.RelTotaleSospeso, Is.EqualTo(500m));
            Assert.That(riga.Iva, Is.EqualTo(22m));
        });
    }

    // --- funzionali: regola per tipologia --------------------------------------------------------

    /// <summary>Ogni saldo prende la REL della propria tipologia, anche quando nel mese c'e' il PRIMO SALDO.</summary>
    [TestCase(SecondoSaldo)]
    [TestCase(SemSospesi)]
    [TestCase(VarSemestrale)]
    public async Task Saldo_PrendeLaRelDellaPropriaTipologia(string tipologia)
    {
        var id = await Emessa(tipologia);
        await Sospesa(id, tipologia, Mese);
        await Rel(PrimoSaldo, totale: 1000m);
        await Rel(tipologia, totale: 2000m);

        var riga = (await Leggi(id)).Single();

        Assert.That(riga.RelTotale, Is.EqualTo(2000m));
    }

    /// <summary>ACCONTO e ANTICIPO non hanno una REL propria: prendono quella del PRIMO SALDO del mese.</summary>
    [TestCase("ACCONTO", SemSospesi)]
    [TestCase("ACCONTO", SecondoSaldo)]
    [TestCase("ANTICIPO", SemSospesi)]
    [TestCase("ANTICIPO", SecondoSaldo)]
    [TestCase("ANTICIPO", VarSemestrale)]
    public async Task AccontoAnticipo_PrendonoLaRelDelPrimoSaldo(string tipologia, string altraRel)
    {
        var id = await Emessa(tipologia);
        await Sospesa(id, tipologia, Mese);
        await Rel(PrimoSaldo, totale: 1000m);
        await Rel(altraRel, totale: 3000m);

        var riga = (await Leggi(id)).Single();

        Assert.That(riga.RelTotale, Is.EqualTo(1000m));
    }

    /// <summary>La stessa regola vale per la REL temporanea di ogni sospesa, ACCONTO e ANTICIPO compresi.</summary>
    [TestCase(PrimoSaldo, PrimoSaldo)]
    [TestCase(SecondoSaldo, SecondoSaldo)]
    [TestCase("ACCONTO", PrimoSaldo)]
    [TestCase("ANTICIPO", PrimoSaldo)]
    public async Task RelTemporanea_DellaTipologiaDellaSospesa(string tipologiaSospesa, string relAttesa)
    {
        var id = await Emessa(tipologiaSospesa);
        await Sospesa(id, tipologiaSospesa, Mese);
        foreach (var (tipologia, totale) in new[] { (PrimoSaldo, 100m), (SecondoSaldo, 200m), (SemSospesi, 300m) })
            await RelTmp(tipologia, totale);
        var attesa = relAttesa == PrimoSaldo ? 100m : 200m;

        var riga = (await Leggi(id)).Single();

        Assert.That(riga.RelTotaleSospeso, Is.EqualTo(attesa));
    }

    // --- adversarial -----------------------------------------------------------------------------

    /// <summary>
    /// Nessun ripiego (decisione del team DATA): se nel mese manca la REL attesa, la testata resta senza
    /// REL anche se ce n'e' un'altra. Sono i 205 anticipi del 2026/6 con la sola SEM. SOSPESI.
    /// </summary>
    [TestCase("ANTICIPO", SemSospesi)]
    [TestCase("ACCONTO", SemSospesi)]
    [TestCase("ANTICIPO", VarSemestrale)]
    [TestCase(SemSospesi, PrimoSaldo)]
    [TestCase(SecondoSaldo, PrimoSaldo)]
    public async Task RelAttesaAssente_TotaliAZeroSenzaPrendereLAltra(string tipologia, string unicaRel)
    {
        var id = await Emessa(tipologia);
        await Sospesa(id, tipologia, Mese);
        await Rel(unicaRel, totale: 1000m, caricata: 1);

        var riga = (await Leggi(id)).Single();

        Assert.Multiple(() =>
        {
            Assert.That(riga.RelTotale, Is.EqualTo(0m));
            Assert.That(riga.RelTotaleAnalogico, Is.EqualTo(0m));
            Assert.That(riga.RelTotaleNotifiche, Is.EqualTo(0));
            Assert.That(riga.Caricata, Is.Null);
        });
    }

    /// <summary>Senza alcuna REL le righe ci sono comunque: la LEFT JOIN non deve diventare una INNER JOIN.</summary>
    [Test]
    public async Task NessunaRel_LeSospeseCompaionoComunque()
    {
        var id = await Emessa(PrimoSaldo);
        await Sospesa(id, PrimoSaldo, 5);
        await Sospesa(id, PrimoSaldo, Mese);

        var righe = await Leggi(id);

        Assert.That(righe, Has.Count.EqualTo(2));
        Assert.That(righe.Select(r => (r.RelTotale, r.RelTotaleSospeso)), Is.All.EqualTo((0m, (decimal?)0m)));
    }

    /// <summary>Una REL della tipologia giusta ma di un altro periodo, contratto o ente non va agganciata.</summary>
    [TestCase("altro mese")]
    [TestCase("altro anno")]
    [TestCase("altro contratto")]
    [TestCase("altro ente")]
    public async Task RelDellaTipologiaGiusta_MaFuoriChiave_NonAgganciata(string caso)
    {
        var id = await Emessa(PrimoSaldo);
        await Sospesa(id, PrimoSaldo, Mese);
        var (ente, contratto, anno, mese) = caso switch
        {
            "altro mese" => (Ente, Contratto, Anno, Mese - 1),
            "altro anno" => (Ente, Contratto, Anno - 1, Mese),
            "altro contratto" => (Ente, "TOKEN-E1-PF976", Anno, Mese),
            _ => (AltroEnte, Contratto, Anno, Mese)
        };
        await Rel(PrimoSaldo, totale: 1000m, ente: ente, contratto: contratto, anno: anno, mese: mese);
        await RelTmp(PrimoSaldo, totale: 500m, ente: ente, contratto: contratto, anno: anno, mese: mese);

        var riga = (await Leggi(id)).Single();

        Assert.That((riga.RelTotale, riga.RelTotaleSospeso), Is.EqualTo((0m, (decimal?)0m)));
    }

    /// <summary>
    /// Il caso piu' sfavorevole: tutte le tipologie di REL nel mese, sia definitive sia temporanee, per piu'
    /// sospese. Il numero di righe resta quello delle sospese, qualunque sia la tipologia della fattura.
    /// </summary>
    [TestCase(PrimoSaldo)]
    [TestCase("ANTICIPO")]
    [TestCase(SemSospesi)]
    public async Task TutteLeRelNelMese_UnaRigaPerSospesa(string tipologia)
    {
        var id = await Emessa(tipologia);
        foreach (var mese in new[] { 4, 5, 6 })
            await Sospesa(id, tipologia, mese);
        foreach (var rel in new[] { PrimoSaldo, SecondoSaldo, SemSospesi, VarSemestrale, "ACCONTO", "ANTICIPO" })
        {
            await Rel(rel, totale: 100m);
            foreach (var mese in new[] { 4, 5, 6 })
                await RelTmp(rel, totale: 100m, mese: mese);
        }

        Assert.That(await Leggi(id), Has.Count.EqualTo(3));
    }

    /// <summary>Le due rotte (admin ed ente) usano la stessa query e devono restituire le stesse righe.</summary>
    [Test]
    public async Task RotteAdminEdEnte_StesseRighe()
    {
        var id = await Emessa(PrimoSaldo);
        await Sospesa(id, PrimoSaldo, 5);
        await Sospesa(id, PrimoSaldo, Mese);
        await Rel(PrimoSaldo, totale: 1000m);
        await Rel(SecondoSaldo, totale: 7777m);

        var admin = await Leggi(id);
        var ente = (await new FattureDettaglioEmessoPersistence(
                new FattureDocContabileDettaglioEmessoQuery(new AuthenticationInfo { IdEnte = Ente }) { IdFattura = id })
            .Execute(_connection, "pfd", _transaction)).ToList();

        Assert.That(ente.Select(r => (r.IdFatturaSospesa, r.RelTotale)),
            Is.EquivalentTo(admin.Select(r => (r.IdFatturaSospesa, r.RelTotale))));
    }

    [TestCase("ente di un'altra fattura")]
    [TestCase("fattura inesistente")]
    [TestCase("ente ostile")]
    public async Task ParametriNonCoerenti_NessunaRiga(string caso)
    {
        var id = await Emessa(PrimoSaldo);
        await Sospesa(id, PrimoSaldo, Mese);
        await Rel(PrimoSaldo, totale: 1000m);

        var righe = caso switch
        {
            "ente di un'altra fattura" => await Leggi(id, AltroEnte),
            "fattura inesistente" => await Leggi(-id),
            _ => await Leggi(id, "' OR 1=1 --")
        };

        Assert.That(righe, Is.Empty);
    }

    /// <summary>
    /// **Potenziale difetto noto, fuori dal perimetro di PF-976**: `pfd.Contratti` e' agganciato per solo
    /// ente, quindi un ente con due contratti raddoppia le righe. Oggi nessuno dei 5 enti con due contratti
    /// ha fatture emesse; correggerlo su `onboardingtokenid` toglierebbe il tipo contratto all'unica fattura
    /// senza corrispondenza. Il test descrive il comportamento corretto e resta ignorato finche' non si decide.
    /// </summary>
    [Test]
    [Ignore("Potenziale difetto noto: join pfd.Contratti per solo ente (PF-976, fuori perimetro)")]
    public async Task EnteConDueContratti_NonRaddoppiaLeRighe()
    {
        await _connection.ExecuteAsync("""
            INSERT INTO pfd.Contratti (internalistitutionid, FkIdTipoContratto, onboardingtokenid)
            VALUES (@ente, 1, 'TOKEN-E1-PF976')
            """, new { ente = Ente }, _transaction);
        var id = await Emessa(PrimoSaldo);
        await Sospesa(id, PrimoSaldo, Mese);

        Assert.That(await Leggi(id), Has.Count.EqualTo(1));
    }

    // ---------------------------------------------------------------------------------------------

    private async Task<List<FatturaDocContabileEmessoDettaglioDto>> Leggi(long idFattura, string ente = Ente) =>
        (await new FattureDettaglioEmessoAdminPersistence(new FattureDocContabileDettaglioAdminEmessoQuery
        {
            IdEnte = ente,
            IdFattura = idFattura
        }).Execute(_connection, "pfd", _transaction)).ToList();

    private Task<long> Emessa(string tipologia) =>
        _connection.QuerySingleAsync<long>("""
            INSERT INTO pfd.FattureTestata
                (FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura, IdentificativoFattura,
                 TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento, MeseRiferimento, CodiceContratto,
                 Progressivo, FatturaInviata)
            OUTPUT INSERTED.IdFattura
            VALUES ('prod-pn', 'TD01', @tipologia, @ente, '2094-07-15', 'PF976', 1000, 'EUR', 'B30', @anno, @mese,
                    @contratto, 976, 0)
            """, new { tipologia, ente = Ente, contratto = Contratto, anno = Anno, mese = Mese }, _transaction);

    /// <summary>Sospesa consolidata nella fattura emessa tramite la tabella ponte `pfd.MesiFatture`.</summary>
    private async Task<long> Sospesa(long idEmessa, string tipologia, int mese)
    {
        var id = await _connection.QuerySingleAsync<long>("""
            INSERT INTO pfd.tmpFattureTestata
                (FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura, IdentificativoFattura,
                 TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento, MeseRiferimento, CodiceContratto,
                 Progressivo, FatturaInviata, FlagFatturata)
            OUTPUT INSERTED.IdFattura
            VALUES ('prod-pn', 'TD01', @tipologia, @ente, '2094-07-01', 'PF976-TMP', 100, 'EUR', 'B30', @anno, @mese,
                    @contratto, 9760 + @mese, 0, 1)
            """, new { tipologia, ente = Ente, contratto = Contratto, anno = Anno, mese }, _transaction);
        await _connection.ExecuteAsync("""
            INSERT INTO pfd.MesiFatture (FkIdEnte, AnnoRiferimento, MeseRiferimento, FKTipologiaFattura, FkIdFattura, FkIdFatturaTmp, FlagEliminata)
            VALUES (@ente, @anno, @mese, @tipologia, @idEmessa, @id, 0)
            """, new { ente = Ente, anno = Anno, mese, tipologia, idEmessa, id }, _transaction);
        return id;
    }

    private Task Rel(string tipologia, decimal totale, byte caricata = 1, bool relFatturata = true,
        string ente = Ente, string contratto = Contratto, int anno = Anno, int mese = Mese) =>
        InsertRel("pfd.RelTestata", tipologia, totale, caricata, relFatturata, ente, contratto, anno, mese);

    private Task RelTmp(string tipologia, decimal totale, string ente = Ente, string contratto = Contratto,
        int anno = Anno, int mese = Mese) =>
        InsertRel("pfd.tmpRelTestata", tipologia, totale, 0, false, ente, contratto, anno, mese);

    private Task InsertRel(string tabella, string tipologia, decimal totale, byte caricata, bool relFatturata,
        string ente, string contratto, int anno, int mese) =>
        _connection.ExecuteAsync($"""
            INSERT INTO {tabella}
                (internal_organization_id, contract_id, TipologiaFattura, [year], [month], TotaleAnalogico, TotaleDigitale,
                 TotaleNotificheAnalogiche, TotaleNotificheDigitali, Totale, TotaleAnalogicoIva, TotaleDigitaleIva, TotaleIva,
                 Caricata, RelFatturata)
            VALUES (@ente, @contratto, @tipologia, @anno, @mese, @totale * 0.4, @totale * 0.6,
                    4, 6, @totale, @totale * 0.488, @totale * 0.732, @totale * 1.22, @caricata, @relFatturata)
            """, new { ente, contratto, tipologia, anno, mese, totale, caricata, relFatturata }, _transaction);

    private Task Iva(int anno, int mese) =>
        _connection.ExecuteAsync("INSERT INTO pfd.Iva (Anno, Mese, Iva) VALUES (@anno, @mese, 22)",
            new { anno, mese }, _transaction);
}
