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
/// Regola (dal 25/11/2026): <c>NULL</c> se posticipata/eliminata (Stato 0 o 3) in <c>cfg.GestioneFatture</c>;
/// <c>ELIMINA</c> per il PRIMO SALDO dell'ente INPS; altrimenti l'azione di <c>be.vwGestioneFattureFormAnniMesi</c>
/// per tipologia/anno/mese (ELIMINA su ANTICIPO/ACCONTO, POSTICIPA sulle altre), <c>NULL</c> fuori finestra.
/// Lo stato di invio a SAP non conta. Il campo <c>fattura.stato</c> riporta <c>gf.Stato</c>. Dati e motivazione
/// delle date relative in <see cref="FattureAzioneTestData"/>.
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
        Assert.Multiple(() =>
        {
            Assert.That(fattura!.Azione, Is.EqualTo(caso.AzioneAttesa),
                $"{caso} ({periodo.Mese}/{periodo.Anno}, oggi {_oggi:MM/yyyy}): azione sbagliata.");
            Assert.That(fattura.Stato, Is.EqualTo(caso.StatoGestione),
                $"{caso}: fattura.stato deve essere lo Stato di cfg.GestioneFatture, NULL se la fattura non c'e'.");
        });
    }

    [TestCase(7824)]
    [TestCase(7855)] // INPS PRIMO SALDO: l'eccezione non riporta in griglia una posticipata
    public async Task Posticipata_ShouldNonComparireNellaGriglia_NeConNeSenzaAzione(long id)
    {
        var caso = CasoDi(id);
        var righe = await Query(Periodo(_oggi, caso));
        Assert.That(righe.Select(x => x.IdFattura), Does.Not.Contain(id),
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
        // L'ente INPS e' escluso: il suo PRIMO SALDO ha l'eccezione, coperta piu' sotto.
        var inps = Casi(_oggi).Where(c => c.Ente == Inps).Select(c => c.Id).ToHashSet();
        var righe = (await Query((_oggi.Year, _oggi.Month), tipologia: [tipologia]))
            .Where(x => x.IdFattura is >= 7801 and <= 7899 && !inps.Contains(x.IdFattura)).ToList();

        Assert.That(righe, Is.Not.Empty, $"Nessuna fattura di test {tipologia} nel mese corrente.");
        Assert.That(righe.Select(x => x.Azione), Is.All.EqualTo(attesa),
            "ELIMINA solo su ANTICIPO e ACCONTO, POSTICIPA su tutte le altre tipologie.");
    }

    // ============================== Eccezione INPS ==============================

    [Test]
    public async Task PrimoSaldo_StessoMese_ShouldEssereEliminaPerInps_EPosticipaPerGliAltri()
    {
        // Il contrasto rende il test significativo: stessa tipologia, stesso periodo, enti diversi.
        var righe = await Query((_oggi.Year, _oggi.Month), tipologia: ["PRIMO SALDO"]);

        Assert.Multiple(() =>
        {
            Assert.That(righe.Single(x => x.IdFattura == 7851).Azione, Is.EqualTo(Elimina), "INPS: eccezione.");
            Assert.That(righe.Single(x => x.IdFattura == 7803).Azione, Is.EqualTo(Posticipa), "Altro ente: la vista.");
        });
    }

    [Test]
    public async Task EccezioneInps_ShouldValereSoloPerIlPrimoSaldo()
    {
        var righe = (await Query((_oggi.Year, _oggi.Month))).ToDictionary(x => x.IdFattura, x => x.Azione);

        Assert.Multiple(() =>
        {
            Assert.That(righe[7858], Is.EqualTo(Posticipa), "SECONDO SALDO INPS: niente eccezione.");
            Assert.That(righe[7859], Is.EqualTo(Elimina), "ANTICIPO INPS: ELIMINA dalla vista, come per tutti.");
        });
    }

    [Test]
    public async Task EccezioneInps_FuoriFinestra_ShouldDareComunqueElimina()
    {
        // Comportamento del codice al 25/11/2026: la condizione INPS viene prima di vam.Azione e non guarda la
        // finestra. Il commento nella query parla di "ELIMINA se non esiste la REL", ma la REL non e' verificata:
        // se la regola va ristretta, questo test va aggiornato insieme alla query.
        var periodo = Periodo(_oggi, CasoDi(7852));
        var f = (await Query(periodo)).Single(x => x.IdFattura == 7852);
        Assert.That(f.Azione, Is.EqualTo(Elimina));
    }

    // ============================== Stato di invio ==============================

    [TestCase(7811, Posticipa)]
    [TestCase(7813, Elimina)]
    [TestCase(7853, Elimina)] // INPS PRIMO SALDO
    public async Task FiltroInviate_ShouldRestituireLaFatturaInviataConLAzione(long id, string attesa)
    {
        // Fino al 25/11/2026 una fattura gia' inviata a SAP non aveva azione; la regola e' stata tolta.
        var periodo = Periodo(_oggi, CasoDi(id));
        var f = (await Query(periodo, fatturaInviata: 1)).SingleOrDefault(x => x.IdFattura == id);

        Assert.That(f, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(f!.Inviata, Is.EqualTo(1));
            Assert.That(f.Azione, Is.EqualTo(attesa), "Lo stato di invio non toglie piu' l'azione.");
        });
    }

    [Test]
    public async Task Inviata_FuoriFinestra_ShouldRestareSenzaAzione()
    {
        var f = (await Query(Periodo(_oggi, CasoDi(7814)), fatturaInviata: 1)).Single(x => x.IdFattura == 7814);
        Assert.That(f.Azione, Is.Null, "Togliere il controllo sull'invio non apre la finestra: decide ancora la vista.");
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
        // 6001 (SECONDO SALDO 2024/3, gia' inviata): dal 25/11/2026 l'invio non conta, resta NULL perche' fuori
        // finestra. Controllo di regressione sul seed storico, che non deve "acquistare" un'azione.
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
