using MediatR;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// L'ultimo tratto della catena che produce il "Report di dettaglio notifiche": non *quali righe*
/// vengono estratte (quello lo coprono `RelRigheFiltroPeriodoIntegrationTests` e la fixture
/// adversarial) ma **cosa contiene il file** che l'aderente riceve.
///
///     seed -> RelRigheQueryGetById -> ToStream&lt;RigheRelDto, RigheRelDtoPagoPAMap&gt; -> testo CSV
///
/// È esattamente la sequenza di `CreateRelRighe.RunAsync`, meno l'ultimo passo: `AddDocument`, che
/// carica lo stream su blob storage. Quello resta fuori — richiederebbe Azurite nel compose e non
/// contiene logica di business, salvo la composizione del nome del file.
///
/// Perché vale la pena arrivare fin qui invece di fermarsi al DTO: fra la query e il file c'è una
/// mappa CsvHelper con indici espliciti (`RigheRelDtoPagoPAMap`) e una configurazione di cultura. Una
/// colonna rimossa dalla mappa, o un cambio di delimitatore, non fa fallire nessun test sulla query e
/// nessuna eccezione a runtime: produce un CSV che il destinatario legge male. Questo è il livello a
/// cui la specifica del team DATA è verificabile su ciò che esce davvero.
///
/// Gira sul DB seedato; container spento -> i test si ignorano.
/// </summary>
public class RelRigheCsvContenutoIntegrationTests
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
    // Forma del file
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Il formato è deciso in `DocsExtensions`: delimitatore **`;`**, intestazione presente, cultura
    /// **it-IT** (quindi la virgola decimale). Sono tre scelte che nessun altro test presidia e che il
    /// destinatario del file vedrebbe subito, ma solo aprendolo.
    /// </summary>
    [Test]
    public async Task Csv_ShouldAvereIntestazionePuntoEVirgolaEImportiInFormatoItaliano()
    {
        var csv = await Csv("SECONDO SALDO", 2026, 5);
        var righe = Linee(csv);

        Assert.Multiple(() =>
        {
            Assert.That(righe[0], Does.Contain("contract_id;"), "delimitatore ';' e intestazione presente");
            Assert.That(righe[0], Does.Contain("event_id"));
            Assert.That(righe[0], Does.Contain("Tipologia Fattura"));
            Assert.That(righe[1], Does.Contain("7,00"), "cultura it-IT: il costo usa la virgola decimale");
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Il contenuto rispetta il filtro di periodo della specifica DATA
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// PRIMO SALDO: anno/mese, e con l'asseverazione dello stesso periodo inclusa (l'OR esplicito
    /// riservato a questa tipologia). Tre righe di dati, più l'intestazione.
    /// </summary>
    [Test]
    public async Task Csv_PrimoSaldo_ShouldContenereLeRigheDelPeriodoPiuLAsseverazione()
    {
        var csv = await Csv("PRIMO SALDO", 2026, 5);

        Assert.Multiple(() =>
        {
            Assert.That(csv, Does.Contain("REL-PS-1"));
            Assert.That(csv, Does.Contain("REL-PS-2"));
            Assert.That(csv, Does.Contain("REL-ASS-1"));
            Assert.That(Linee(csv), Has.Length.EqualTo(4), "intestazione + 3 righe");
        });
    }

    /// <summary>
    /// VAR. SEMESTRALE: l'unica tipologia per cui il file contiene **tutto il semestre**, quindi anche
    /// il mese diverso da quello richiesto.
    /// </summary>
    [Test]
    public async Task Csv_VarSemestrale_ShouldContenereTuttoIlSemestre()
    {
        var csv = await Csv("VAR. SEMESTRALE", 2026, 5);

        Assert.Multiple(() =>
        {
            Assert.That(csv, Does.Contain("REL-VS-MAG"));
            Assert.That(csv, Does.Contain("REL-VS-GIU"));
            Assert.That(Linee(csv), Has.Length.EqualTo(3), "intestazione + 2 righe");
        });
    }

    /// <summary>
    /// SEM. SOSPESI: il contrasto con il test precedente. Stessa forma nel seed — due mesi, stesso
    /// `FlagConguaglio` — ma nel file finisce il **solo mese richiesto**, perché le sue righe
    /// conservano il periodo di riferimento originale.
    /// </summary>
    [Test]
    public async Task Csv_SemSospesi_ShouldContenereSoloIlMeseRichiesto()
    {
        var csv = await Csv("SEM. SOSPESI", 2026, 5);

        Assert.Multiple(() =>
        {
            Assert.That(csv, Does.Contain("REL-SS-MAG"));
            Assert.That(csv, Does.Not.Contain("REL-SS-GIU"));
            Assert.That(Linee(csv), Has.Length.EqualTo(2), "intestazione + 1 riga");
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Mappatura delle colonne
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Le colonne di dettaglio che la mappa dichiara e che nessun test sulla query vedrebbe sparire:
    /// se una venisse rimossa da `RigheRelDtoPagoPAMap`, la query continuerebbe a restituire il DTO
    /// completo e solo il file uscirebbe mutilato.
    ///
    /// Il seed valorizza apposta CAP, recapitista e destinatario sulle righe SECONDO SALDO.
    /// </summary>
    [Test]
    public async Task Csv_ShouldMappareLeColonneDiDettaglioDellaRiga()
    {
        var csv = await Csv("SECONDO SALDO", 2026, 5);
        var intestazione = Linee(csv)[0];
        var riga = Linee(csv)[1];

        Assert.Multiple(() =>
        {
            Assert.That(intestazione, Does.Contain("zip_code"));
            Assert.That(intestazione, Does.Contain("recapitista"));
            Assert.That(intestazione, Does.Contain("recipient_id"));
            Assert.That(riga, Does.Contain("00100"));
            Assert.That(riga, Does.Contain("Recapitista Uno"));
            Assert.That(riga, Does.Contain("RCP-1"));
        });
    }

    /// <summary>
    /// Un periodo senza righe produce un file con la **sola intestazione**, non un file vuoto né
    /// un'eccezione: è ciò che verrebbe caricato sul blob, quindi vale la pena saperlo.
    /// </summary>
    [Test]
    public async Task Csv_PeriodoSenzaRighe_ShouldContenereLaSolaIntestazione()
    {
        var csv = await Csv("PRIMO SALDO", 2026, 9);

        Assert.That(Linee(csv), Has.Length.EqualTo(1), "la sola riga di intestazione");
    }

    // ---------------------------------------------------------------------------------------------

    /// <summary>Ricompone la stessa catena di `CreateRelRighe.RunAsync`, fino allo stream CSV.</summary>
    private async Task<string> Csv(string tipologia, int anno, int mese)
    {
        var chiave = $"{Ente}_{Contratto}_{tipologia.Replace(" ", "-")}_{anno}_{mese}";

        var righe = await _handler.Send(new RelRigheQueryGetById(Auth(Ente)) { IdTestata = chiave });

        var stream = await righe!.ToStream<RigheRelDto, RigheRelDtoPagoPAMap>();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static string[] Linee(string csv) =>
        csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static AuthenticationInfo Auth(string idEnte) => new()
    {
        Id = "integration-test-relrighe-csv",
        IdEnte = idEnte,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };
}
