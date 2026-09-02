using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// `NotificaQueryGetByListEntiPersistencev2` — la ricerca notifiche lato admin servita da
/// **`POST api/v2/notifiche/pagopa`**. E' la gemella della v1 (coperta da
/// <see cref="NotificaQueryListaEntiIntegrationTests"/>): stesso SQL, stessi filtri, stesso DTO.
///
/// L'unica differenza sta in COME viene eseguita. La v1 passa i parametri a Dapper
/// (`QueryMultipleAsync(sql, parameters)`); la v2 costruisce a mano un `SqlCommand` con una lista di
/// `SqlParameter` e mappa il `DataReader` colonna per colonna. E' una riscrittura, non un'evoluzione
/// funzionale — quindi il modo di testarla e' il **confronto con la v1**, non la riverifica di ogni
/// filtro.
///
/// Questo file copre cio' che la v2 fa correttamente. Le divergenze — che sono parecchie e non tutte
/// innocue — stanno in <see cref="NotificaQueryListaEntiV2AdversarialTests"/> e nella sezione finale
/// qui sotto.
///
/// Seed condiviso con la v1 (tests/Data/notifiche.sql), ente1 / TOKEN-E1, periodo 2026/3:
///   EVT-3001  AR   (analogica)   CAP 00100  REC-3001  non contestata
///   EVT-3002  890  (analogica)   CAP 20100  REC-3002  CONTESTATA, flag 3, con CodiceOggetto
///   EVT-3003  digitale (paper_product_type NULL, number_of_pages NULL)  non contestata
/// </summary>
public class NotificaQueryListaEntiV2IntegrationTests
{
    private const string Ente = "11111111-1111-1111-1111-111111111111";
    private const int Anno = 2026;
    private const int Mese = 3;

    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    // ---------------------------------------------------------------------------------------------
    // Il periodo: l'unico filtro che la v2 sa applicare davvero (v. la suite adversarial)
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task Periodo_ShouldRestituireLeTreNotificheDelMese_EIlCountCoerente()
    {
        var r = await Cerca(q => { });

        Assert.Multiple(() =>
        {
            Assert.That(Ids(r), Is.EquivalentTo(new[] { "EVT-3001", "EVT-3002", "EVT-3003" }));
            Assert.That(r!.Count, Is.EqualTo(3),
                "Il Count arriva dal SECONDO result set dello stesso comando: se diverge dalle righe, "
                + "il DataReader non sta avanzando correttamente con NextResultAsync.");
        });
    }

    [Test]
    public async Task PeriodoSenzaDati_ShouldRestituireListaVuota_ECountZero()
    {
        var r = await Cerca(q => { q.AnnoValidita = 1999; q.MeseValidita = 1; });

        Assert.Multiple(() =>
        {
            Assert.That(r!.Notifiche, Is.Empty);
            Assert.That(r.Count, Is.Zero);
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Paginazione: insieme al periodo, l'unica cosa parametrizzata
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Il Count deve restare quello TOTALE anche quando la pagina ne mostra meno: e' il numero con cui
    /// il frontend calcola quante pagine esistono. Se seguisse la pagina, la griglia mostrerebbe
    /// sempre una pagina sola.
    /// </summary>
    [Test]
    public async Task Paginazione_ShouldLimitareLeRigheMaNonIlCount()
    {
        var pagina1 = await Cerca(q => { q.Page = 1; q.Size = 2; });
        var pagina2 = await Cerca(q => { q.Page = 2; q.Size = 2; });

        Assert.Multiple(() =>
        {
            Assert.That(Ids(pagina1), Has.Count.EqualTo(2));
            Assert.That(Ids(pagina2), Has.Count.EqualTo(1));
            Assert.That(pagina1!.Count, Is.EqualTo(3), "Il Count non deve seguire la pagina.");
            Assert.That(Ids(pagina1).Intersect(Ids(pagina2)), Is.Empty, "Le pagine non devono sovrapporsi.");
        });
    }

    // ---------------------------------------------------------------------------------------------
    // L'unico filtro oltre al periodo che la v2 sopravvive: stato = [1]
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Caso speciale e non ovvio: chiedendo **solo** lo stato 1 ("Non Contestata") la persistence non
    /// aggiunge un `IN`, ma un `t.FKIdFlagContestazione is NULL` — che non ha parametri. E' quindi
    /// l'unico filtro di lista che nella v2 non esplode (v. suite adversarial): non perche' sia
    /// gestito, ma perche' per caso non gli serve un parametro.
    /// </summary>
    [Test]
    public async Task StatoSoloNonContestata_ShouldSelezionareLeNotificheSenzaRigaDiContestazione()
    {
        var r = await Cerca(q => q.StatoContestazione = [1]);

        Assert.That(Ids(r), Is.EquivalentTo(new[] { "EVT-3001", "EVT-3003" }),
            "Lo stato 1 e' l'ASSENZA di riga in pfw.Contestazioni, non un valore memorizzato.");
    }

    // ---------------------------------------------------------------------------------------------
    // La mappatura a mano del DataReader: 38 assegnazioni scritte una per una
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Dove la v1 si affida a Dapper, la v2 legge `reader["Colonna"]` per ogni proprieta'. Un alias
    /// rinominato nel SELECT non diventa piu' un `null` silenzioso come con Dapper: diventa una
    /// `IndexOutOfRangeException`, cioe' un 500. In compenso una proprieta' **dimenticata** resta
    /// muta per sempre — ed e' successo tre volte (v. sezione finale).
    ///
    /// Questo test fissa i campi che vengono da JOIN diversi, cioe' quelli che si romperebbero per
    /// primi: la contestazione (`pfw.Contestazioni`), la sua tipologia (`pfw.TipoContestazione`) e il
    /// flag (`pfw.FlagContestazione`).
    /// </summary>
    [Test]
    public async Task NotificaContestata_ShouldPortareTuttiICampiDeiJoin()
    {
        var r = await Cerca(q => { });
        var n = r!.Notifiche!.Single(x => x.IdNotifica == "EVT-3002");

        Assert.Multiple(() =>
        {
            Assert.That(n.IUN, Is.EqualTo("IUN-3002"));
            Assert.That(n.CAP, Is.EqualTo("20100"));
            Assert.That(n.IdEnte, Is.EqualTo(Ente));
            Assert.That(n.Anno, Is.EqualTo("2026"), "Anno e Mese sono STRINGHE sul DTO, non interi.");
            Assert.That(n.Mese, Is.EqualTo("3"));
            Assert.That(n.StatoContestazione, Is.EqualTo(3), "Letto con GetByte: la colonna e' tinyint.");
            Assert.That(n.Contestazione, Is.EqualTo("Contestata Ente"));
            Assert.That(n.TipoContestazione, Is.EqualTo("Mancato recapito"));
            Assert.That(n.NoteEnte, Is.EqualTo("Notifica mai recapitata"));
            Assert.That(n.Onere, Is.EqualTo("Recapitista"));
            Assert.That(n.Fatturata, Is.False);
        });
    }

    /// <summary>
    /// La notifica non contestata passa dagli stessi LEFT JOIN, ma senza riga corrispondente: i campi
    /// della contestazione devono essere nulli e lo stato ricostruito a 1 dall'`ISNULL` della query.
    /// </summary>
    [Test]
    public async Task NotificaNonContestata_ShouldAvereStatoUnoECampiContestazioneNulli()
    {
        var r = await Cerca(q => { });
        var n = r!.Notifiche!.Single(x => x.IdNotifica == "EVT-3001");

        Assert.Multiple(() =>
        {
            Assert.That(n.StatoContestazione, Is.EqualTo(1));
            Assert.That(n.NoteEnte, Is.Null);
            Assert.That(n.Onere, Is.Null);
            Assert.That(n.TipoContestazione, Is.EqualTo("Non Contestata"),
                "Non e' null: la query fa ISNULL(a.TipoContestazione, f.FlagContestazione).");
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Parita' con la v1, che e' il vero criterio di questa riscrittura
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task StessoPeriodo_ShouldDareLoStessoInsiemeDellaV1()
    {
        var v2 = await Cerca(q => { });
        var v1 = await _handler.Send(new NotificaQueryGetByListaEnti(Auth())
        {
            AnnoValidita = Anno,
            MeseValidita = Mese
        });

        Assert.Multiple(() =>
        {
            Assert.That(Ids(v2), Is.EquivalentTo(v1!.Notifiche!.Select(x => x.IdNotifica)));
            Assert.That(v2!.Count, Is.EqualTo(v1.Count));
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Regressioni rispetto alla v1 nella sola mappatura (i filtri sono nella suite adversarial)
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// ⚠️ DIFETTO. Il SELECT restituisce `NotificationType`, `IdTipoContestazione` e `CodiceOggetto`,
    /// il DTO ha le tre proprieta' — ma la mappatura a mano della v2 **non le assegna**. Con Dapper
    /// (v1) si popolavano da sole; qui restano a null/0 per sempre, in silenzio.
    ///
    /// Non e' cosmetico: `CodiceOggetto` e `Id Tipo Contestazione` sono due colonne dell'export Excel
    /// delle notifiche (v. gli `HeaderAttributev2` sul DTO). Sulla v2 escono vuote.
    /// </summary>
    [Ignore("DIFETTO APERTO — la mappatura manuale del DataReader dimentica NotificationType, "
        + "IdTipoContestazione e CodiceOggetto: sono nel SELECT e nel DTO, ma nessuno le assegna. "
        + "Con Dapper (v1) erano popolate. Togliere questo attributo quando le tre assegnazioni "
        + "saranno aggiunte in NotificaQueryGetByListEntiPersistencev2.")]
    [Test]
    public async Task CampiDelSelect_ShouldEssereMappatiComeNellaV1()
    {
        var r = await Cerca(q => { });
        var n = r!.Notifiche!.Single(x => x.IdNotifica == "EVT-3002");

        Assert.Multiple(() =>
        {
            Assert.That(n.CodiceOggetto, Is.EqualTo("CODOGG-3002"), "Colonna dell'export Excel.");
            Assert.That(n.IdTipoContestazione, Is.EqualTo(1));
            Assert.That(n.NotificationType, Is.EqualTo("890"));
        });
    }

    /// <summary>
    /// ⚠️ DIFETTO piu' sottile del precedente. `NumberOfPages` e `CostEuroInCentesimi` sono stringhe
    /// sul DTO e la v2 le riempie con `reader["…"].ToString()`. Su una colonna NULL quel `ToString()`
    /// non da' `null`: da' **stringa vuota** (`DBNull.Value.ToString()`). Dapper, nella v1, lasciava
    /// `null`.
    ///
    /// La differenza conta per chi consuma il payload: `null` e `""` si serializzano in JSON in modo
    /// diverso, e un frontend che fa un controllo di presenza cambia comportamento senza che nulla
    /// segnali il cambio. Il caso concreto e' la notifica digitale, che non ha numero di pagine.
    /// </summary>
    [Ignore("DIFETTO APERTO — reader[...].ToString() su una colonna NULL produce stringa vuota invece "
        + "di null (la v1 con Dapper dava null). Riguarda NumberOfPages e CostEuroInCentesimi. "
        + "Togliere questo attributo quando la conversione gestira' DBNull esplicitamente.")]
    [Test]
    public async Task ColonneNumericheNulle_ShouldRestareNullComeNellaV1()
    {
        var r = await Cerca(q => { });
        var digitale = r!.Notifiche!.Single(x => x.IdNotifica == "EVT-3003");

        Assert.That(digitale.NumberOfPages, Is.Null,
            "La notifica digitale non ha number_of_pages: deve arrivare null, non \"\".");
    }

    // ---------------------------------------------------------------------------------------------

    /// <summary>Ricerca v2 sul periodo del seed, con le personalizzazioni del caso.</summary>
    private async Task<Infrastructure.Common.SEND.Notifiche.Dto.NotificaDto?> Cerca(
        Action<NotificaQueryGetByListaEntiv2> personalizza)
    {
        var query = new NotificaQueryGetByListaEntiv2(Auth())
        {
            AnnoValidita = Anno,
            MeseValidita = Mese
        };
        personalizza(query);
        return await _handler.Send(query);
    }

    private static List<string> Ids(Infrastructure.Common.SEND.Notifiche.Dto.NotificaDto? r) =>
        r?.Notifiche?.Select(x => x.IdNotifica ?? string.Empty).ToList() ?? [];

    private static AuthenticationInfo Auth() => new()
    {
        Id = "integration-test-notifiche-v2",
        IdEnte = Ente,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };
}
