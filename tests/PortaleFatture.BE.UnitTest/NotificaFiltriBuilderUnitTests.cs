using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// `NotificaFiltriSQLBuilder` — la composizione del WHERE della ricerca notifiche, estratta il
/// 31/08/2026 dalle due Persistence che la duplicavano parola per parola (v1 con Dapper, v2 con un
/// `SqlCommand` costruito a mano).
///
/// **Perche' questi test esistono.** La riscrittura v2 ha perso dieci parametri su quattordici: al
/// comando ne arrivano quattro, mentre il WHERE continua a contenere tutti i segnaposto. Il difetto e'
/// invisibile in review — l'insieme completo era ancora li', riga per riga, in un `ExpandoObject` che
/// nessuno leggeva piu' — e finora si vedeva solo eseguendo la query contro un database vero, dove si
/// manifesta come `Must declare the scalar variable`.
///
/// L'invariante che lo avrebbe intercettato sta in due righe e non richiede alcun DB:
///
///     ogni `@segnaposto` presente nel WHERE deve avere il suo parametro
///
/// I test sono scritti sull'**insieme** dei segnaposto, non su un elenco compilato a mano: un filtro
/// aggiunto domani entra da solo nella verifica.
///
/// Nota di visibilita': `NotificaFiltriSQLBuilder` e' `public` mentre `NotificaSQLBuilder` (la SELECT,
/// l'ORDER BY, l'OFFSET) resta `internal` e Infrastructure non espone i propri internal ai progetti di
/// test — motivo per cui l'ordinamento continua a essere verificato solo per i suoi effetti, negli
/// integration test.
/// </summary>
public class NotificaFiltriBuilderUnitTests
{
    // =============================================================================================
    // L'invariante
    // =============================================================================================

    /// <summary>
    /// Ogni combinazione plausibile di filtri: il WHERE non deve mai citare un parametro che poi non
    /// viene fornito. E' la regola che la v2 viola a valle (v. la sezione dedicata sotto).
    /// </summary>
    [TestCaseSource(nameof(CombinazioniDiFiltri))]
    public void Componi_OgniSegnapostoDelWhere_ShouldAvereIlSuoParametro(string descrizione, NotificaFiltriInput input)
    {
        var filtri = NotificaFiltriSQLBuilder.Componi(input);

        var mancanti = NotificaFiltriSQLBuilder.Segnaposto(filtri.Where)
            .Where(s => !filtri.Parametri.ContainsKey(s))
            .ToArray();

        Assert.That(mancanti, Is.Empty,
            $"[{descrizione}] il WHERE cita {string.Join(", ", mancanti)} senza fornirne il valore. "
            + $"WHERE: {filtri.Where}");
    }

    [Test]
    public void Componi_SenzaAlcunFiltro_ShouldProdurreWhereVuotoESenzaParametri()
    {
        var filtri = NotificaFiltriSQLBuilder.Componi(new NotificaFiltriInput());

        Assert.Multiple(() =>
        {
            Assert.That(filtri.Where, Is.Empty);
            Assert.That(filtri.Parametri, Is.Empty);
        });
    }

    [Test]
    public void Componi_ListeVuote_ShouldEssereComeNonFiltrare()
    {
        // `IsNullNotAny()` e' vera sia per null sia per l'array vuoto: un IN vuoto NON restringe, non
        // azzera. E' coerente in tutti i filtri a lista del progetto, quindi non e' una svista — ma e'
        // l'opposto di cio' che ci si aspetta, ed e' il tipo di cosa che una "pulizia" cambierebbe.
        var vuoti = NotificaFiltriSQLBuilder.Componi(new NotificaFiltriInput
        {
            EntiIds = [],
            Recapitisti = [],
            Consolidatori = [],
            TipoNotifica = [],
            StatoContestazione = []
        });

        Assert.That(vuoti.Where, Is.Empty);
        Assert.That(vuoti.Parametri, Is.Empty);
    }

    // =============================================================================================
    // Le due costruzioni fragili ma corrette
    // =============================================================================================

    [Test]
    public void Componi_SoloDigitali_ShouldTradursiInIsNull_NonInUnCodice()
    {
        // TipoNotifica.Digitali mappa sulla STRINGA VUOTA, non su un codice: viene scartata dalla
        // lista dei paper_product_type, e la sola cosa che resta e' l'IS NULL. Chi "normalizzasse"
        // quel Map() cambierebbe il risultato della ricerca senza che nulla fallisca.
        var filtri = NotificaFiltriSQLBuilder.Componi(new NotificaFiltriInput
        {
            AnnoValidita = 2026,
            TipoNotifica = [TipoNotifica.Digitali]
        });

        Assert.That(filtri.Where, Does.Contain("paper_product_type IS NULL"));
        Assert.That((IEnumerable<string?>)filtri.Parametri["TipoNotifica"], Is.Empty,
            "La lista dei codici resta vuota: Digitali non ne ha uno.");
    }

    [Test]
    public void Componi_DigitaliPiuAnalogico_ShouldTenereEntrambiIRami()
    {
        var filtri = NotificaFiltriSQLBuilder.Componi(new NotificaFiltriInput
        {
            AnnoValidita = 2026,
            TipoNotifica = [TipoNotifica.Digitali, TipoNotifica.Analogico890]
        });

        Assert.That(filtri.Where, Does.Contain("paper_product_type IN @tipoNotifica")
            .And.Contain("OR paper_product_type IS NULL"));
        Assert.That((IEnumerable<string?>)filtri.Parametri["TipoNotifica"], Is.EquivalentTo(new[] { "890" }));
    }

    [Test]
    public void Componi_SoloNonContestata_ShouldTradursiInIsNull_SenzaIn()
    {
        // Lo stato 1 non esiste in pfw.Contestazioni: e' il default di chi NON ha una riga. Va quindi
        // tradotto in un IS NULL, non in un IN — altrimenti la ricerca "non contestate" non trova nulla.
        var filtri = NotificaFiltriSQLBuilder.Componi(new NotificaFiltriInput
        {
            AnnoValidita = 2026,
            StatoContestazione = [1]
        });

        Assert.That(filtri.Where, Does.Contain("t.FKIdFlagContestazione is NULL"));
        Assert.That(filtri.Where, Does.Not.Contain("IN @contestazione"));
    }

    [Test]
    public void Componi_NonContestataPiuAltriStati_ShouldUnireIsNullEIn()
    {
        var filtri = NotificaFiltriSQLBuilder.Componi(new NotificaFiltriInput
        {
            AnnoValidita = 2026,
            StatoContestazione = [1, 3]
        });

        Assert.That(filtri.Where,
            Does.Contain("(t.FKIdFlagContestazione is NULL OR t.FKIdFlagContestazione IN @contestazione)"));
        Assert.That(filtri.Parametri.ContainsKey("Contestazione"), Is.True);
    }

    // =============================================================================================
    // Il difetto che l'estrazione rende visibile: la v2 non passa i parametri
    // =============================================================================================

    /// <summary>
    /// DIFETTO APERTO (regressione v2) — questo e' il test che avrebbe intercettato la riscrittura.
    ///
    /// Il WHERE e' lo stesso della v1, ma al `SqlCommand` arrivano solo `@Page/@Size/@Anno/@Mese`:
    /// qualunque altro filtro produce `Must declare the scalar variable`. Con un solo filtro oltre al
    /// periodo il test e' gia' rosso.
    ///
    /// Chi ripara la v2 passa l'intero `filtri.Parametri` ed espande le liste (v. il test successivo),
    /// toglie questo `[Ignore]` e chiude insieme i sei test disattivati in
    /// `NotificaQueryListaEntiV2*IntegrationTests`.
    /// </summary>
    [Test]
    [Ignore("DIFETTO APERTO (regressione v2) — al SqlCommand arrivano solo Page/Size/Anno/Mese: ogni "
        + "altro filtro nel WHERE resta senza parametro. Rimedio: passare tutto filtri.Parametri ed "
        + "espandere le liste degli IN. V. coverage/test-backlog.md, sezione 'Ricerca notifiche v2'.")]
    public void ParametriComandoV2_ShouldCoprireTuttiISegnapostoDelWhere()
    {
        var filtri = NotificaFiltriSQLBuilder.Componi(FiltriCompleti());
        var passati = NotificaFiltriSQLBuilder.NomiParametriComandoV2(filtri);

        var mancanti = NotificaFiltriSQLBuilder.Segnaposto(filtri.Where)
            .Where(s => !passati.Contains(s, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        Assert.That(mancanti, Is.Empty);
    }

    /// <summary>
    /// CARATTERIZZAZIONE del comportamento odierno, cosi' il difetto resta misurato e non solo
    /// dichiarato: quattro parametri passati a fronte di dodici segnaposto nel WHERE.
    /// Quando diventa rosso, la v2 e' stata riparata: cancellare questo test e togliere gli `[Ignore]`.
    /// </summary>
    [Test]
    public void ParametriComandoV2_OggiNePassaSoloQuattro_Caratterizzazione()
    {
        var filtri = NotificaFiltriSQLBuilder.Componi(FiltriCompleti());

        Assert.Multiple(() =>
        {
            Assert.That(NotificaFiltriSQLBuilder.NomiParametriComandoV2(filtri),
                Is.EquivalentTo(new[] { "Page", "Size", "Anno", "Mese" }));
            Assert.That(filtri.Parametri, Has.Count.GreaterThan(4),
                "Il builder li produce tutti: e' la v2 a scartarli, non il builder a non fornirli.");
        });
    }

    /// <summary>
    /// DIFETTO APERTO (regressione v2), secondo aspetto — `IN @lista` non e' T-SQL: e' una comodita' di
    /// **Dapper**, che a runtime espande la lista in `IN (@lista1, @lista2, …)`. Un `SqlCommand`
    /// costruito a mano deve farlo da se', altrimenti l'errore non e' nemmeno "parametro mancante" ma
    /// `Incorrect syntax near '@…'`.
    ///
    /// Il test e' scritto sul contratto atteso: nessun `IN @nome` deve sopravvivere nel SQL che la v2
    /// manda al comando.
    /// </summary>
    [Test]
    [Ignore("DIFETTO APERTO (regressione v2) — i filtri a lista restano come 'IN @lista', sintassi "
        + "valida solo per Dapper. Rimedio: espandere le liste in IN (@n0, @n1, ...) quando si "
        + "costruisce il SqlCommand. V. coverage/test-backlog.md.")]
    public void ComandoV2_ShouldNonContenereIndicatoriDiListaNonEspansi()
    {
        var filtri = NotificaFiltriSQLBuilder.Componi(FiltriCompleti());

        Assert.That(filtri.Where, Does.Not.Match(@"IN\s+@\w+"),
            "Un IN con parametro non espanso arriva al server come sintassi non valida.");
    }

    // =============================================================================================
    // Il difetto condiviso da entrambe le versioni
    // =============================================================================================

    /// <summary>
    /// DIFETTO APERTO (v1 **e** v2) — la parola `WHERE` la emette **solo** il filtro sull'anno; tutti
    /// gli altri aggiungono `" AND …"`. Senza anno, quell'`AND` non finisce in un WHERE inesistente:
    /// si attacca all'ultima riga della SELECT, che e' un `LEFT JOIN … ON …`, diventando parte della
    /// condizione di join — dove non elimina nemmeno una riga.
    ///
    /// E' il difetto peggiore dell'area perche' **non fallisce**: cercare una notifica per IUN senza
    /// indicare l'anno — l'uso piu' naturale che esista — restituisce l'intero insieme invece di
    /// quella notifica.
    ///
    /// Rimedio: accumulare le condizioni in una lista e comporre il WHERE alla fine
    /// (`conditions.Any() ? " WHERE " + string.Join(" AND ", conditions) : ""`), invece di legare
    /// l'emissione della parola WHERE a un filtro particolare.
    /// </summary>
    [TestCase("iun", TestName = "FiltroSenzaAnno_ShouldEmettereWhere(iun)")]
    [TestCase("cap", TestName = "FiltroSenzaAnno_ShouldEmettereWhere(cap)")]
    [TestCase("mese", TestName = "FiltroSenzaAnno_ShouldEmettereWhere(mese)")]
    [TestCase("enti", TestName = "FiltroSenzaAnno_ShouldEmettereWhere(enti)")]
    [Ignore("DIFETTO APERTO (v1 e v2) — senza AnnoValidita il frammento inizia per ' AND ' e finisce "
        + "nella ON dell'ultima LEFT JOIN, dove non filtra: cercare per IUN senza anno restituisce "
        + "tutte le notifiche. Rimedio: comporre il WHERE con un accumulatore di condizioni. "
        + "V. coverage/test-backlog.md.")]
    public void FiltroSenzaAnno_ShouldEmettereWhere(string filtro)
    {
        var input = filtro switch
        {
            "iun" => new NotificaFiltriInput { Iun = "IUN-1" },
            "cap" => new NotificaFiltriInput { Cap = "00100" },
            "mese" => new NotificaFiltriInput { MeseValidita = 3 },
            _ => new NotificaFiltriInput { EntiIds = ["ente-1"] }
        };

        var filtri = NotificaFiltriSQLBuilder.Componi(input);

        Assert.That(filtri.Where.TrimStart(), Does.StartWith("WHERE"));
    }

    /// <summary>
    /// CARATTERIZZAZIONE del comportamento odierno. Quando diventa rosso il difetto e' stato chiuso:
    /// cancellare questo test e togliere l'`[Ignore]` da quello gemello.
    /// </summary>
    [Test]
    public void FiltroSenzaAnno_OggiProduceUnAndOrfano_Caratterizzazione()
    {
        var filtri = NotificaFiltriSQLBuilder.Componi(new NotificaFiltriInput { Iun = "IUN-1" });

        Assert.Multiple(() =>
        {
            Assert.That(filtri.Where, Does.Not.Contain("WHERE"));
            Assert.That(filtri.Where.TrimStart(), Does.StartWith("AND"));
            Assert.That(filtri.Parametri.ContainsKey("Iun"), Is.True,
                "Il parametro c'e': e' la condizione a non essere in un WHERE.");
        });
    }

    // =============================================================================================
    // Dati
    // =============================================================================================

    private static NotificaFiltriInput FiltriCompleti() => new()
    {
        AnnoValidita = 2026,
        MeseValidita = 3,
        Page = 1,
        Size = 50,
        Prodotto = "prod-pn",
        Cap = "00100",
        Profilo = "PA",
        Iun = "IUN-1",
        RecipientId = "REC-1",
        EntiIds = ["ente-1", "ente-2"],
        Recapitisti = ["rec-1"],
        Consolidatori = ["cons-1"],
        TipoNotifica = [TipoNotifica.Analogico890],
        StatoContestazione = [3]
    };

    private static IEnumerable<TestCaseData> CombinazioniDiFiltri()
    {
        yield return new TestCaseData("solo periodo",
            new NotificaFiltriInput { AnnoValidita = 2026, MeseValidita = 3 });
        yield return new TestCaseData("periodo + paginazione",
            new NotificaFiltriInput { AnnoValidita = 2026, Page = 1, Size = 50 });
        yield return new TestCaseData("tutti i filtri", FiltriCompleti());
        yield return new TestCaseData("solo liste",
            new NotificaFiltriInput
            {
                AnnoValidita = 2026,
                EntiIds = ["e1"],
                Recapitisti = ["r1"],
                Consolidatori = ["c1"]
            });
        yield return new TestCaseData("solo digitali",
            new NotificaFiltriInput { AnnoValidita = 2026, TipoNotifica = [TipoNotifica.Digitali] });
        yield return new TestCaseData("digitali + analogiche",
            new NotificaFiltriInput
            {
                AnnoValidita = 2026,
                TipoNotifica = [TipoNotifica.Digitali, TipoNotifica.AnalogicoARNazionali]
            });
        yield return new TestCaseData("solo non contestate",
            new NotificaFiltriInput { AnnoValidita = 2026, StatoContestazione = [1] });
        yield return new TestCaseData("non contestate + contestate",
            new NotificaFiltriInput { AnnoValidita = 2026, StatoContestazione = [1, 3] });
        yield return new TestCaseData("solo contestate",
            new NotificaFiltriInput { AnnoValidita = 2026, StatoContestazione = [3, 4] });
        yield return new TestCaseData("ricerca testuale",
            new NotificaFiltriInput { AnnoValidita = 2026, Iun = "IUN-1", RecipientId = "REC-1", Cap = "00100" });
    }
}
