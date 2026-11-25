using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using static PortaleFatture.BE.IntegrationTest.FattureAzioneTestData;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// PF-1003: casi avversari sul campo <c>fattura.azione</c> di <c>POST api/fatture</c>. Cercano i modi in cui
/// la join con la vista o con <c>cfg.GestioneFatture</c> potrebbe duplicare fatture, attribuire lo stato di
/// un'altra fattura, o dipendere da dettagli fragili (maiuscole, parametri anomali).
/// </summary>
[Category("Fatture")]
[NonParallelizable]
public class FattureRicercaAzioneAdversarialIntegrationTests
{
    private IMediator _handler = null!;
    private DateTime _oggi;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        SqlMapper.AddTypeHandler(typeof(FattureListaDto), new JsonTypeHandler());
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _oggi = OggiDb(LocalTestDb.ConnectionString);
        Inserisci(LocalTestDb.ConnectionString, _oggi);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        try { Pulisci(LocalTestDb.ConnectionString); } catch (SqlException) { /* container spento */ }
    }

    private static AuthenticationInfo AdminAuth() => new()
    { IdEnte = Guid.NewGuid().ToString(), Prodotto = "prod-pn", Ruolo = Ruolo.ADMIN, IdTipoContratto = 1 };

    private async Task<List<TitoloFatturaDto>> Query(int anno, int mese, string[]? tipologia = null)
    {
        var res = await _handler.Send(new FattureQueryRicerca(AdminAuth())
        { Cancellata = false, Anno = anno, Mese = mese, TipologiaFattura = tipologia });
        return (res ?? new FattureListaDto()).Select(x => x.fattura!).ToList();
    }

    private Task<List<TitoloFatturaDto>> MeseCorrente(string[]? tipologia = null) => Query(_oggi.Year, _oggi.Month, tipologia);

    private static async Task Esegui(string sql, object param)
    {
        await using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        await conn.ExecuteAsync(sql, param);
    }

    // ============================== Duplicazioni ==============================

    [Test]
    public async Task DueFattureStessoPeriodo_ShouldComparireUnaVoltaCiascuna_ConLaStessaAzione()
    {
        var righe = (await MeseCorrente()).Where(x => x.IdFattura is 7804 or 7807).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(righe.Select(x => x.IdFattura), Is.EquivalentTo(new long[] { 7804, 7807 }),
                "La join con la vista non deve moltiplicare le fatture che condividono ente/tipologia/periodo.");
            Assert.That(righe.Select(x => x.Azione), Is.All.EqualTo(Posticipa));
        });
    }

    [Test]
    public async Task MeseCorrente_ShouldRestituireOgniFatturaDiTestUnaSolaVolta()
    {
        var ids = (await MeseCorrente()).Select(x => x.IdFattura).Where(id => id is >= 7801 and <= 7899).ToList();
        var attesi = Casi(_oggi).Where(c => c.OffsetMesi == 0 && c.StatoGestione != 0).Select(c => c.Id);

        Assert.That(ids, Is.EquivalentTo(attesi), "Nessuna fattura mancante e nessun doppione.");
    }

    [Test]
    [Ignore("Difetto preesistente, non del PF-1003: la PK di cfg.GestioneFatture include lo Stato, quindi due righe "
        + "per lo stesso periodo sono ammesse e la LEFT JOIN di _sqlView duplica la fattura. Rimedio proposto: "
        + "OUTER APPLY (SELECT TOP 1 ...) o NOT EXISTS, come nell'export (FattureRelExcelBuilder._sqlRel). Da "
        + "concordare prima di cambiarlo: tocca anche l'esclusione delle posticipate.")]
    public async Task DueRigheGestioneStessoPeriodo_ShouldNonDuplicareLaFattura()
    {
        var (anno, mese) = Periodo(_oggi, Casi(_oggi).Single(c => c.Id == 7822));
        await Esegui("""
            INSERT INTO cfg.GestioneFatture (FkIdEnte, FkTipologiaFattura, Anno, Mese, IdUtenteInserimento, Stato, Azione)
            VALUES (@ente, 'ACCONTO', @anno, @mese, @marcatore, 1, 'RIPRISTINATA')
            """, new { ente = Ente3, anno, mese, marcatore = Marcatore });

        var righe = (await Query(anno, mese)).Where(x => x.IdFattura == 7822).ToList();
        Assert.That(righe, Has.Count.EqualTo(1));
    }

    // ============================== Lo stato di un'altra fattura non conta ==============================

    [Test]
    public async Task StatoGestioneDiUnAltroEnte_ShouldNonTogliereLAzione()
    {
        // Riga di Gestione Fatture sullo stesso periodo e tipologia di 7804, ma per l'ente 3: la chiave
        // e' ente+tipologia+anno+mese, quindi 7804 (ente 1) deve conservare la sua azione.
        await Esegui("""
            INSERT INTO cfg.GestioneFatture (FkIdEnte, FkTipologiaFattura, Anno, Mese, IdUtenteInserimento, Stato, Azione)
            VALUES (@ente, 'SECONDO SALDO', @anno, @mese, @marcatore, 3, 'ELIMINATA')
            """, new { ente = Ente3, anno = _oggi.Year, mese = _oggi.Month, marcatore = Marcatore });
        try
        {
            var f = (await MeseCorrente()).Single(x => x.IdFattura == 7804);
            Assert.That(f.Azione, Is.EqualTo(Posticipa));
        }
        finally
        {
            await Esegui("DELETE FROM cfg.GestioneFatture WHERE FkIdEnte = @ente AND FkTipologiaFattura = 'SECONDO SALDO' AND Anno = @anno AND Mese = @mese AND IdUtenteInserimento = @marcatore",
                new { ente = Ente3, anno = _oggi.Year, mese = _oggi.Month, marcatore = Marcatore });
        }
    }

    [Test]
    public async Task StatoGestioneSullaStessaFatturaDiAltroMese_ShouldNonTogliereLAzione()
    {
        // 7823 (SEM. SOSPESI ente 1, mese -1) e' eliminata; 7806 (stesso ente e tipologia, mese corrente) no.
        var f = (await MeseCorrente()).Single(x => x.IdFattura == 7806);
        Assert.That(f.Azione, Is.EqualTo(Posticipa));
    }

    [Test]
    public async Task StatoGestioneConCasingEnteDiverso_ShouldTogliereComunqueLAzione()
    {
        // Il GUID ente in Gestione Fatture puo' arrivare in maiuscolo (era la causa del "404 fantasma",
        // PF-672). La join e' case-insensitive: la riga va riconosciuta lo stesso.
        await Esegui("""
            INSERT INTO cfg.GestioneFatture (FkIdEnte, FkTipologiaFattura, Anno, Mese, IdUtenteInserimento, Stato, Azione)
            VALUES (UPPER(@ente), 'VAR. SEMESTRALE', @anno, @mese, @marcatore, 3, 'ELIMINATA')
            """, new { ente = Ente1, anno = _oggi.Year, mese = _oggi.Month, marcatore = Marcatore });
        try
        {
            var f = (await MeseCorrente()).Single(x => x.IdFattura == 7805);
            Assert.That(f.Azione, Is.Null);
        }
        finally
        {
            await Esegui("DELETE FROM cfg.GestioneFatture WHERE FkTipologiaFattura = 'VAR. SEMESTRALE' AND Anno = @anno AND Mese = @mese AND IdUtenteInserimento = @marcatore",
                new { anno = _oggi.Year, mese = _oggi.Month, marcatore = Marcatore });
        }
    }

    // ============================== Formato del valore ==============================

    [Test]
    public async Task Azione_ShouldEssereSempreUnoDeiDueValoriMaiuscoliONull()
    {
        // La vista scrive la tipologia in Title Case ('Secondo Saldo') e la join funziona per la collation
        // case-insensitive; il valore dell'azione invece deve arrivare esattamente come lo legge il frontend.
        var periodi = Casi(_oggi).Select(c => Periodo(_oggi, c)).Distinct();
        var valori = new List<string?>();
        foreach (var (anno, mese) in periodi)
            valori.AddRange((await Query(anno, mese)).Select(x => x.Azione));

        Assert.That(valori.Where(v => v is not null), Is.All.EqualTo(Elimina).Or.EqualTo(Posticipa));
    }

    [Test]
    public async Task TipologiaSalvataInMinuscolo_ShouldAvereComunqueLAzione()
    {
        // Robustezza sulla collation: se una fattura avesse la tipologia in minuscolo, il match con la vista
        // (Title Case) deve reggere come oggi regge quello con FatturaTestataConfig (maiuscolo).
        await Esegui("UPDATE pfd.FattureTestata SET FkTipologiaFattura = 'secondo saldo' WHERE IdFattura = 7807", new { });
        try
        {
            var f = (await MeseCorrente()).Single(x => x.IdFattura == 7807);
            Assert.That(f.Azione, Is.EqualTo(Posticipa));
        }
        finally
        {
            await Esegui("UPDATE pfd.FattureTestata SET FkTipologiaFattura = 'SECONDO SALDO' WHERE IdFattura = 7807", new { });
        }
    }

    // ============================== Parametri anomali ==============================

    [Test]
    public async Task FiltroTipologia_ShouldNonCambiareLAzione()
    {
        var tutte = (await MeseCorrente()).Where(x => x.IdFattura is >= 7801 and <= 7899).ToDictionary(x => x.IdFattura, x => x.Azione);
        var filtrate = (await MeseCorrente(["ANTICIPO", "SECONDO SALDO"])).Where(x => x.IdFattura is >= 7801 and <= 7899).ToList();

        Assert.That(filtrate, Is.Not.Empty);
        Assert.That(filtrate.All(x => tutte[x.IdFattura] == x.Azione), Is.True,
            "Il filtro tipologia restringe le righe, non deve alterare l'azione di quelle che restano.");
    }

    [Test]
    public async Task TipologiaInesistente_ShouldRestituireVuotoSenzaErrori()
        => Assert.That(await MeseCorrente(["__NESSUNA__"]), Is.Empty);

    [TestCase(1999, 13)]
    [TestCase(2026, 0)]
    [TestCase(9999, 12)]
    public async Task PeriodoAssurdo_ShouldRestituireVuotoSenzaErrori(int anno, int mese)
        => Assert.That(await Query(anno, mese), Is.Empty);
}
