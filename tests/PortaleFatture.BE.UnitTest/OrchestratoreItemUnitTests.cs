using System.Text.Json;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// La logica **pura** dell'area Orchestratore: la traduzione del codice di esecuzione nella sua
/// descrizione, che è l'unico calcolo che il backend fa su questi dati (il resto è una lettura secca
/// della vista `pfd.vOrchestratore`, coperta a livello integration).
///
/// `OrchestratoreItem.DescrizioneEsecuzione` è una **proprietà calcolata senza setter**: non viene mai
/// assegnata da Dapper, viene valutata quando qualcuno la legge — cioè al momento della
/// serializzazione JSON della risposta e durante l'export Excel (è decorata con `HeaderAttributev2`,
/// quindi la reflection dell'export la include). È il motivo per cui vale la pena testarla da sola:
/// un difetto qui non si manifesta dove è scritto, ma nel serializzatore.
///
/// Nota di perimetro: `OrchestratoreSQLBuilder` è `internal` e Infrastructure non espone i propri
/// internal ai progetti di test (solo `SendEmailFunction` lo fa), quindi la composizione dell'ORDER BY
/// e dell'OFFSET si verifica per i suoi effetti negli integration test — stesso limite già registrato
/// per `NotificaSQLBuilder`.
/// </summary>
public class OrchestratoreItemUnitTests
{
    // ---------------------------------------------------------------------------------------------
    // La tabella degli stati, che è l'unica fonte di verità di questa traduzione
    // ---------------------------------------------------------------------------------------------

    [TestCase(0, "Programmato")]
    [TestCase(1, "Eseguito")]
    [TestCase(2, "Eseguito no data")]
    [TestCase(3, "Errore")]
    public void DescrizioneEsecuzione_ShouldTradurreIQuattroStatiNoti(int esecuzione, string atteso)
        => Assert.That(new OrchestratoreItem { Esecuzione = esecuzione }.DescrizioneEsecuzione,
            Is.EqualTo(atteso));

    /// <summary>
    /// Gli stati sono **hardcoded in C#** (`StatiQuery.GetStati()`), non letti da una lookup a
    /// database: la colonna `Esecuzione` della vista è popolata dalle pipeline del team Data, quindi
    /// un quinto stato può comparire senza che il backend ne sappia nulla. Questo test fissa il
    /// perimetro attuale — se il team Data ne aggiunge uno, va aggiornato il dizionario, e questa
    /// asserzione è il posto in cui ci si accorge che il numero è cambiato.
    /// </summary>
    [Test]
    public void GetStati_ShouldContenereEsattamenteIQuattroStatiPrevisti()
    {
        var stati = StatiQuery.GetStati();

        Assert.Multiple(() =>
        {
            Assert.That(stati, Has.Count.EqualTo(4));
            Assert.That(stati.Keys, Is.EquivalentTo(new[] { 0, 1, 2, 3 }));
        });
    }

    /// <summary>
    /// Uno stato sconosciuto degrada correttamente: `TryGetValue` fallisce e la proprietà vale `null`,
    /// senza eccezioni. È il comportamento giusto — e rende ancora più stridente il caso `null` qui
    /// sotto, che con lo stesso identico intento produce invece un'eccezione.
    /// </summary>
    [TestCase(4)]
    [TestCase(99)]
    [TestCase(-1)]
    public void DescrizioneEsecuzione_StatoSconosciuto_ShouldEssereNullSenzaEccezioni(int esecuzione)
        => Assert.That(new OrchestratoreItem { Esecuzione = esecuzione }.DescrizioneEsecuzione, Is.Null);

    // ---------------------------------------------------------------------------------------------
    // Il caso NULL — che la vista attuale NON può produrre (v. sotto)
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// ATTENZIONE DIFETTO, ma oggi non raggiungibile dalla vista. `Esecuzione` è dichiarata `int?` — quindi il
    /// modello **prevede** che possa essere nulla — ma il getter fa `Esecuzione!.Value`: l'operatore
    /// `!` mette a tacere il compilatore senza garantire nulla, e a runtime `.Value` su un `int?`
    /// vuoto solleva `InvalidOperationException`.
    ///
    /// **Rettifica del 31/08/2026**, dopo aver messo `pfd.vOrchestratore` nel DB seedato e averne
    /// letto la DDL reale da produzione: tutti e otto i rami dell'UNION calcolano `Esecuzione` con un
    /// `CASE` che ha sempre un `ELSE` costante, e i metadati della vista la dichiarano infatti
    /// **NOT NULL**. Il difetto resta nel C# — il tipo dice una cosa e il getter ne assume un'altra —
    /// ma da questa vista non lo si innesca: prima si pensava fosse "quello che la vista può davvero
    /// restituire", e non è così.
    ///
    /// Resta quindi un rischio latente, non un bug in produzione: basterebbe che un nono ramo, o una
    /// modifica lato team Data, introducesse un `NULL` perché l'INTERA risposta di
    /// `POST api/orchestratore` diventi un 500 (la proprietà la legge il serializzatore, non
    /// l'handler). Il fatto che nessuna riga lo produca è verificato end-to-end da
    /// `OrchestratoreQueryIntegrationTests.OgniRiga_ShouldAvereUnaDescrizioneEsecuzione`.
    /// </summary>
    [Ignore("DIFETTO APERTO ma NON raggiungibile dalla vista attuale (verificato 31/08/2026: "
        + "pfd.vOrchestratore dichiara Esecuzione NOT NULL). DescrizioneEsecuzione fa Esecuzione!.Value "
        + "su una proprieta' dichiarata int?: con Esecuzione NULL solleva InvalidOperationException "
        + "invece di restituire null, come fa gia' per uno stato sconosciuto. Togliere questo attributo "
        + "quando il getter gestira' il null, oppure chiudere la discrepanza rendendo Esecuzione non "
        + "nullable, visto che la vista lo garantisce.")]
    [Test]
    public void DescrizioneEsecuzione_EsecuzioneNulla_ShouldEssereNull()
        => Assert.That(new OrchestratoreItem { Esecuzione = null }.DescrizioneEsecuzione, Is.Null);

    [Test]
    public void DescrizioneEsecuzione_EsecuzioneNulla_OggiSolleva_Caratterizzazione()
    {
        var item = new OrchestratoreItem { Esecuzione = null };

        Assert.Throws<InvalidOperationException>(() => _ = item.DescrizioneEsecuzione,
            "Se questo test diventa rosso il getter e' stato reso sicuro: togliere l'[Ignore] del "
            + "test gemello e cancellare questa caratterizzazione.");
    }

    /// <summary>
    /// Dove il difetto si manifesta davvero. La proprietà non viene letta dal codice dell'handler: la
    /// legge il **serializzatore**, mentre costruisce la risposta della rotta `POST api/orchestratore`.
    /// Una sola riga con `Esecuzione` nulla nella pagina richiesta non produce quindi un campo vuoto,
    /// ma fa fallire l'intera risposta — 500 su una griglia che ha caricato correttamente i dati.
    ///
    /// Lo stesso vale per il download `POST api/orchestratore/download`, dove la reflection
    /// dell'export Excel legge la proprietà per la colonna "Esecuzione".
    /// </summary>
    [Test]
    public void SerializzazioneDiUnaRigaConEsecuzioneNulla_OggiFallisce_Caratterizzazione()
    {
        var dto = new OrchestratoreDto
        {
            Count = 1,
            Items = [new OrchestratoreItem { Anno = 2026, Mese = 3, Esecuzione = null }]
        };

        Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(dto),
            "Una riga della vista con Esecuzione NULL fa fallire la risposta intera, non solo il "
            + "proprio campo.");
    }

    /// <summary>
    /// Contro-prova: con `Esecuzione` valorizzata la stessa risposta si serializza, e la descrizione
    /// compare nel JSON. Serve a dimostrare che a rompere è il null e non la forma del DTO.
    /// </summary>
    [Test]
    public void SerializzazioneDiUnaRigaValorizzata_ShouldContenereLaDescrizione()
    {
        var dto = new OrchestratoreDto
        {
            Count = 1,
            Items = [new OrchestratoreItem { Anno = 2026, Mese = 3, Esecuzione = 3 }]
        };

        Assert.That(JsonSerializer.Serialize(dto), Does.Contain("Errore"));
    }
}
