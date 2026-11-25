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
/// PF-1003: integration test (DB seedato) sul campo <c>fattura.azione</c> di <c>POST api/fatture</c>, cioè
/// l'azione che il frontend mostra su ogni fattura della pagina Documenti Emessi. Catena reale:
/// FattureQueryRicerca → handler → FattureQueryRicercaPersistence → <c>FattureQueryRicercaBuilder._sqlView</c>.
///
/// Regola: <c>NULL</c> se la fattura è già inviata a SAP o è posticipata/eliminata (Stato 0 o 3) in <c>cfg.GestioneFatture</c>;
/// altrimenti l'azione di <c>be.vwGestioneFattureFormAnniMesi</c> per tipologia/anno/mese (ELIMINA su
/// ANTICIPO/ACCONTO, POSTICIPA sulle altre), <c>NULL</c> fuori finestra. Dati e motivazione delle date
/// relative in <see cref="FattureAzioneTestData"/>.
/// </summary>
[Category("Fatture")]
[NonParallelizable] // scrive (e poi cancella) righe di configurazione condivise con le altre suite
public class FattureRicercaAzioneIntegrationTests
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
        try { Pulisci(LocalTestDb.ConnectionString); } catch (SqlException) { /* container spento: niente da pulire */ }
    }

    private static AuthenticationInfo AdminAuth() => new()
    { IdEnte = Guid.NewGuid().ToString(), Prodotto = "prod-pn", Ruolo = Ruolo.ADMIN, IdTipoContratto = 1 };

    private async Task<List<TitoloFatturaDto>> Query((int Anno, int Mese) periodo, bool cancellata = false,
        string[]? tipologia = null, int? fatturaInviata = null)
    {
        var res = await _handler.Send(new FattureQueryRicerca(AdminAuth())
        {
            Cancellata = cancellata,
            Anno = periodo.Anno,
            Mese = periodo.Mese,
            TipologiaFattura = tipologia,
            FatturaInviata = fatturaInviata
        });
        return (res ?? new FattureListaDto()).Select(x => x.fattura!).ToList();
    }

    private static IEnumerable<TestCaseData> CasiNellaGriglia() =>
        Casi(DateTime.Today).Where(c => c.StatoGestione != 0)
            .Select(c => new TestCaseData(c.Id).SetName($"Azione_{c.Id}_{c.Tipologia}_{c.Descrizione}"));

    private Caso CasoDi(long id) => Casi(_oggi).Single(c => c.Id == id);

    // ============================== La matrice ==============================

    [TestCaseSource(nameof(CasiNellaGriglia))]
    public async Task Azione_PerOgniCaso_ShouldEssereQuellaAttesa(long id)
    {
        var caso = CasoDi(id);
        var periodo = Periodo(_oggi, caso);
        var fattura = (await Query(periodo)).SingleOrDefault(x => x.IdFattura == id);

        Assert.That(fattura, Is.Not.Null, $"{caso}: la fattura deve comparire tra le emesse di {periodo.Mese}/{periodo.Anno}.");
        Assert.That(fattura!.Azione, Is.EqualTo(caso.AzioneAttesa),
            $"{caso} ({periodo.Mese}/{periodo.Anno}, oggi {_oggi:MM/yyyy}): azione sbagliata.");
    }

    [Test]
    public async Task Posticipata_ShouldNonComparireNellaGriglia_NeConNeSenzaAzione()
    {
        var caso = CasoDi(7824);
        var righe = await Query(Periodo(_oggi, caso));
        Assert.That(righe.Select(x => x.IdFattura), Does.Not.Contain(7824L),
            "Le posticipate (Stato 0) restano escluse dalla griglia emesse, come prima della modifica.");
    }

    // ============================== Regola per tipologia ==============================

    [TestCase("ANTICIPO", Elimina)]
    [TestCase("ACCONTO", Elimina)]
    [TestCase("PRIMO SALDO", Posticipa)]
    [TestCase("SECONDO SALDO", Posticipa)]
    [TestCase("VAR. SEMESTRALE", Posticipa)]
    [TestCase("SEM. SOSPESI", Posticipa)]
    public async Task Azione_MeseCorrente_ShouldDipendereSoloDallaTipologia(string tipologia, string attesa)
    {
        var righe = (await Query((_oggi.Year, _oggi.Month), tipologia: [tipologia]))
            .Where(x => x.IdFattura is >= 7801 and <= 7899).ToList();

        Assert.That(righe, Is.Not.Empty, $"Nessuna fattura di test {tipologia} nel mese corrente.");
        Assert.That(righe.Select(x => x.Azione), Is.All.EqualTo(attesa),
            "ELIMINA solo su ANTICIPO e ACCONTO, POSTICIPA su tutte le altre tipologie.");
    }

    // ============================== Stato di invio ==============================

    [Test]
    public async Task FiltroInviate_ShouldRestituireLaFatturaInviataSenzaAzione()
    {
        var periodo = Periodo(_oggi, CasoDi(7811));
        var inviate = await Query(periodo, fatturaInviata: 1);
        var f = inviate.SingleOrDefault(x => x.IdFattura == 7811);

        Assert.That(f, Is.Not.Null);
        Assert.That(f!.Azione, Is.Null, "Una fattura gia' inviata a SAP non si posticipa ne' si elimina.");
    }

    [Test]
    public async Task FiltroInElaborazione_ShouldRestituireLaFatturaConAzione()
    {
        var periodo = Periodo(_oggi, CasoDi(7812));
        var f = (await Query(periodo, fatturaInviata: 2)).SingleOrDefault(x => x.IdFattura == 7812);

        Assert.That(f, Is.Not.Null, "FatturaInviata NULL = in elaborazione (filtro 2).");
        Assert.Multiple(() =>
        {
            Assert.That(f!.Inviata, Is.EqualTo(2));
            Assert.That(f.Azione, Is.EqualTo(Posticipa), "In elaborazione l'azione resta disponibile (scelta del 24/11/2026).");
        });
    }

    // ============================== Ramo Non Fatturate ==============================

    [Test]
    public async Task NonFatturate_ShouldNonValorizzareMaiAzione()
    {
        // Il ramo Cancellata=true legge be.vwDocumentiEmessiNonFatturati, che non ha la colonna: la posticipata
        // di test 7824 compare li', ma senza azione (Ripristina/Cancella si fanno dalla pagina Gestione Fatture).
        var righe = await Query(Periodo(_oggi, CasoDi(7824)), cancellata: true);

        Assert.That(righe.Select(x => x.IdFattura), Does.Contain(7824L));
        Assert.That(righe.Select(x => x.Azione), Is.All.Null);
    }

    // ============================== Fatture fisse del seed ==============================

    [Test]
    public async Task FatturaDelSeed2024_ShouldNonAvereAzione_PercheFuoriFinestra()
    {
        // 6001 (SECONDO SALDO 2024/3, gia' inviata): doppio motivo per NULL. Resta un controllo di regressione
        // sul seed storico, che non deve "acquistare" un'azione.
        var f = (await Query((2024, 3))).Single(x => x.IdFattura == 6001);
        Assert.That(f.Azione, Is.Null);
    }

    [Test]
    public async Task Ripristinata2024_FuoriFinestra_ShouldNonAvereAzione()
    {
        // 6002: seed storico RIPRISTINATA (Stato 1), periodo 2024/5. Una ripristinata ha l'azione della vista, ma
        // questo periodo e' fuori finestra: NULL. Il caso dentro finestra e' 7821 nella matrice.
        var f = (await Query((2024, 5))).Single(x => x.IdFattura == 6002);
        Assert.That(f.Azione, Is.Null);
    }

    // ============================== La vista del seed allineata a PROD ==============================

    /// <summary>
    /// La vista del seed è una copia scritta a mano: se si discosta da quella reale, i test della matrice
    /// sopra verificano un comportamento che in PROD non c'è (era successo con l'Anticipo, che partiva dal
    /// mese corrente invece che da −2). Offset e azioni da estratto PROD del 24/11/2026.
    /// </summary>
    [TestCase("Anticipo", 2, Elimina)]
    [TestCase("Acconto", 1, Elimina)]
    [TestCase("Primo Saldo", 2, Posticipa)]
    [TestCase("Secondo Saldo", 3, Posticipa)]
    [TestCase("Var. Semestrale", 1, Posticipa)]
    [TestCase("Sem. Sospesi", 1, Posticipa)]
    public async Task VistaSeed_ShouldAvereLaStessaFinestraDiProd(string tipologia, int offset, string azione)
    {
        await using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        var righe = (await conn.QueryAsync<(int Anno, int Mese, string Azione)>(
            "SELECT anno, mese, Azione FROM be.vwGestioneFattureFormAnniMesi WHERE tipologia_fattura = @tipologia",
            new { tipologia })).ToList();

        var primo = new DateTime(_oggi.Year, _oggi.Month, 1).AddMonths(-offset);
        var ultimo = new DateTime(_oggi.Year + 1, 12, 1);
        var attesi = Enumerable.Range(0, (ultimo.Year - primo.Year) * 12 + ultimo.Month - primo.Month + 1)
            .Select(i => primo.AddMonths(i)).Select(d => (d.Year, d.Month)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(righe.Select(r => (r.Anno, r.Mese)), Is.EqualTo(attesi),
                $"{tipologia}: dal mese corrente -{offset} a dicembre dell'anno prossimo, un mese per riga, in ordine.");
            Assert.That(righe.Select(r => r.Azione), Is.All.EqualTo(azione));
        });
    }

    [Test]
    public async Task VistaSeed_ShouldAvereUnaSolaRigaPerTipologiaAnnoMese()
    {
        // La join della query non raggruppa: e' corretta solo se la vista non ha doppioni (vero in PROD).
        await using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        var doppioni = await conn.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM (SELECT tipologia_fattura, anno, mese FROM be.vwGestioneFattureFormAnniMesi
                                  GROUP BY tipologia_fattura, anno, mese HAVING COUNT(*) > 1) d
            """);
        Assert.That(doppioni, Is.Zero);
    }
}
