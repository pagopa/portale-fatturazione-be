using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Test ADVERSARIAL su `RelRigheQueryGetById`, sul modello di `ApiKeysAdversarialIntegrationTests`:
/// non verificano i casi previsti (quelli stanno in `RelRigheFiltroPeriodoIntegrationTests`) ma cosa
/// succede fuori dai binari — periodi impossibili, tipologie inventate, chiavi malformate, valori
/// ostili.
///
/// Perché qui in particolare: la query è invocata dalla **pipeline Synapse** attraverso la Function
/// `CreateRelRighe`, cioè da un chiamante automatico che compone la chiave da dati di configurazione.
/// Un input fuori specifica non produce una segnalazione a video: produce un report mancante, o una
/// Function fallita con un errore che non dice nulla.
///
/// Riferimento: specifica del team DATA dell'08/09/2026. ⚠️ Su un punto va letta con attenzione: i
/// casi limite che cita ("nessun record", "mese fuori range") erano **un'ipotesi di test, non un
/// requisito** — chiarito dal team il 14/09/2026: *"non deve produrre un report vuoto"*. Qui dentro
/// non ci sono quindi aspettative della specifica, ma **caratterizzazioni** del comportamento reale
/// del BE: dicono cosa succede oggi, così un cambiamento si vede.
/// </summary>
public class RelRigheAdversarialIntegrationTests
{
    private const string Ente = "11111111-1111-1111-1111-111111111111";
    private const string Contratto = "TOKEN-E1";

    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    // ---------------------------------------------------------------------------------------------
    // Periodi impossibili: la specifica DATA si attende un report vuoto, il BE solleva
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Un mese fuori range non esiste come periodo, quindi non esiste la sua testata — e l'handler
    /// legge la testata con `FirstOrDefault()!` senza controllo. Il risultato non è una lista vuota:
    /// è una `NullReferenceException`, cioè la Function che fallisce.
    ///
    /// È lo stesso difetto già caratterizzato su un anno inesistente
    /// (`PeriodoSenzaTestata_ShouldThrowNullReference_Caratterizzazione`): qui lo si fissa sui valori
    /// di periodo che un chiamante automatico può comporre per errore.
    ///
    /// Che sia un difetto **non dipende dalla specifica DATA** (che su questo non chiede nulla): un
    /// periodo inesistente è un input plausibile della pipeline, e oggi produce una Function fallita
    /// con un errore che non dice quale sia il problema. Se un domani l'handler gestirà l'assenza di
    /// testata, questo test diventerà rosso — ed è il segnale corretto, non una regressione.
    /// </summary>
    [TestCase(0)]
    [TestCase(13)]
    [TestCase(99)]
    public void MeseFuoriRange_ShouldThrowNullReference_Caratterizzazione(int mese)
    {
        Assert.ThrowsAsync<NullReferenceException>(async () => await Righe("SECONDO SALDO", 2026, mese),
            "Comportamento attuale: nessuna testata per il periodo -> NRE, non lista vuota.");
    }

    /// <summary>
    /// Una tipologia che non esiste finisce nel ramo anno/mese (è la regola generale: tutto ciò che non
    /// è `VAR. SEMESTRALE`), ma non arriva nemmeno lì — la testata non esiste e si ricade nella stessa
    /// eccezione. Detto altrimenti: **il BE non valida la tipologia**, la subisce.
    /// </summary>
    [Test]
    public void TipologiaInesistente_ShouldThrowNullReference_Caratterizzazione()
    {
        Assert.ThrowsAsync<NullReferenceException>(async () => await Righe("PIPPO", 2026, 5),
            "Nessuna validazione della tipologia: si arriva alla ricerca della testata e si fallisce lì.");
    }

    // ---------------------------------------------------------------------------------------------
    // Il casing: dove il nuovo confronto conta davvero
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// La fix confronta la tipologia con `TipologiaFattura.VAR_SEMESTRALE` in
    /// `StringComparison.OrdinalIgnoreCase`. Questo test dice **perché quella scelta non è decorativa**.
    ///
    /// SQL Server ha collation case-insensitive, quindi una tipologia scritta in minuscolo trova
    /// comunque la sua testata e le sue righe. Se il confronto in C# fosse stato `Ordinal`, la stessa
    /// chiamata avrebbe trovato i dati ma preso il ramo **anno/mese**, restituendo un sottoinsieme —
    /// un report incompleto, senza alcun errore. Con `OrdinalIgnoreCase` il risultato è identico a
    /// quello della forma canonica.
    /// </summary>
    [Test]
    public async Task TipologiaConCasingDiverso_ShouldComportarsiComeLaFormaCanonica()
    {
        var righe = await Righe("var. semestrale", 2026, 5);

        Assert.That(righe.Select(r => r.IdNotifica), Is.EquivalentTo(new[] { "REL-VS-MAG", "REL-VS-GIU" }),
            "Con collation SQL case-insensitive i dati si trovano comunque: se il confronto in C# "
            + "tornasse a essere case-sensitive, questa chiamata prenderebbe il ramo anno/mese e "
            + "restituirebbe il solo mese richiesto — un report incompleto, in silenzio.");
    }

    // ---------------------------------------------------------------------------------------------
    // Chiavi malformate
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// `RelTestataKey.Deserialize` fa `Split("_")` e accede a `dati[3]`/`dati[4]` senza verificare
    /// quanti segmenti siano arrivati: una chiave priva di separatori esce con un errore di indice,
    /// non con un messaggio di dominio. Caratterizzazione, non aspettativa.
    /// </summary>
    [Test]
    public void ChiaveSenzaSeparatori_ShouldThrowIndexOutOfRange_Caratterizzazione()
    {
        Assert.ThrowsAsync<IndexOutOfRangeException>(async () =>
            await _handler.Send(new RelRigheQueryGetById(Auth(Ente)) { IdTestata = "chiave-non-valida" }));
    }

    /// <summary>
    /// Stessa funzione, altro punto di rottura: anno e mese passano da `Convert.ToInt32`, quindi un
    /// segmento non numerico dà `FormatException`.
    /// </summary>
    [Test]
    public void ChiaveConAnnoNonNumerico_ShouldThrowFormat_Caratterizzazione()
    {
        var chiave = $"{Ente}_{Contratto}_PRIMO-SALDO_ANNO_5";

        Assert.ThrowsAsync<FormatException>(async () =>
            await _handler.Send(new RelRigheQueryGetById(Auth(Ente)) { IdTestata = chiave }));
    }

    // ---------------------------------------------------------------------------------------------
    // Valori ostili: la query è parametrizzata, restano valori
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Il contratto arriva dalla chiave e finisce in `AND contract_id=@IdContratto`. Con un valore che
    /// prova a chiudere la stringa e aggiungere una condizione sempre vera, l'esito atteso è una lista
    /// **vuota**: se l'iniezione funzionasse, tornerebbero le righe del periodo.
    /// </summary>
    [Test]
    public async Task ContrattoConTentativoDiInjection_ShouldReturnVuoto()
    {
        var chiave = $"{Ente}_TOKEN-E1' OR '1'='1_PRIMO-SALDO_2026_5";

        var righe = await _handler.Send(new RelRigheQueryGetById(Auth(Ente)) { IdTestata = chiave });

        Assert.That(righe, Is.Empty,
            "Il valore resta un valore: nessuna riga, nessun errore SQL. Se qui tornassero righe, "
            + "la condizione iniettata sarebbe stata interpretata.");
    }

    /// <summary>
    /// Contro-prova che i dati siano intatti dopo il tentativo precedente: la stessa query, con una
    /// chiave legittima, continua a restituire il suo contenuto.
    /// </summary>
    [Test]
    public async Task DopoUnTentativoDiInjection_IDatiRestanoIntatti()
    {
        var chiave = $"{Ente}_{Contratto}_PRIMO-SALDO_2026_5'; DROP TABLE pfd.RelRighe--";

        try
        {
            await _handler.Send(new RelRigheQueryGetById(Auth(Ente)) { IdTestata = chiave });
        }
        catch (FormatException)
        {
            // atteso: il mese del segmento finale non è numerico. Ciò che conta è il controllo dopo.
        }

        var righe = await Righe("PRIMO SALDO", 2026, 5);

        Assert.That(righe.Select(r => r.IdNotifica), Does.Contain("REL-PS-1"),
            "La tabella deve esistere ancora e contenere le righe del seed.");
    }

    // ---------------------------------------------------------------------------------------------
    // Il parametro FlagConguaglio del chiamante
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// La specifica DATA prevede un caso "FlagConguaglio malformato". Questo test chiude metà della
    /// domanda: nel BE quel valore **non è un parametro del chiamante**, perché l'handler sovrascrive
    /// sempre quello passato con il valore letto dalla testata.
    ///
    /// Lo dimostra passando l'esempio di valore malformato fornito da DATA (`VAR. SEMESTRALE202607`),
    /// che nel seed non corrisponde ad alcuna riga: se fosse usato, il risultato sarebbe vuoto. Non lo
    /// è — escono le righe del semestre della testata.
    ///
    /// Conseguenza: un flag malformato può arrivare **solo dal database**, non da chi chiama. Cosa
    /// succede in quel caso lo copre l'altra metà,
    /// `RelRigheFiltroPeriodoIntegrationTests.VarSemestrale_TestataConFlagMalformato_*`: report vuoto e
    /// silenzioso, perché il BE tratta il flag come stringa opaca.
    /// </summary>
    [Test]
    public async Task FlagConguaglioPassatoDalChiamante_ShouldEssereIgnorato()
    {
        var chiave = $"{Ente}_{Contratto}_VAR.-SEMESTRALE_2026_5";

        var righe = await _handler.Send(new RelRigheQueryGetById(Auth(Ente))
        {
            IdTestata = chiave,
            FlagConguaglio = "VAR. SEMESTRALE202607"
        });

        Assert.That(righe!.Select(r => r.IdNotifica), Is.EquivalentTo(new[] { "REL-VS-MAG", "REL-VS-GIU" }),
            "Il FlagConguaglio del chiamante è inerte: vince quello della testata. Se questo test "
            + "tornasse vuoto, il parametro avrebbe ripreso effetto e il contratto sarebbe cambiato.");
    }

    // ---------------------------------------------------------------------------------------------

    private async Task<List<RigheRelDto>> Righe(string tipologia, int anno, int mese)
    {
        var chiave = $"{Ente}_{Contratto}_{tipologia.Replace(" ", "-")}_{anno}_{mese}";

        var righe = await _handler.Send(new RelRigheQueryGetById(Auth(Ente)) { IdTestata = chiave });

        return righe?.ToList() ?? [];
    }

    private static AuthenticationInfo Auth(string idEnte) => new()
    {
        Id = "integration-test-relrighe-adv",
        IdEnte = idEnte,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };
}
