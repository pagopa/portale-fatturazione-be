using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// `RelRigheQueryGetById` — l'estrazione delle righe/notifiche di una REL, cioè il contenuto del
/// "Report di dettaglio notifiche". È letta **solo** dalle Azure Function `CreateRelRighe` e
/// `CreateRelSospese` (nessun endpoint la usa: le rotte `.../rel/.../righe` compongono un SAS token
/// verso il CSV già generato, v. `docs/pipeline-dati-send.md`), e non aveva alcun test.
///
/// Il motivo per cui vale più di una copertura qualsiasi: contiene un'invariante che il codice
/// esprime in modo **fragile** e che la documentazione dichiara **corretta**, quindi è esattamente il
/// tipo di cosa che una pulizia ben intenzionata romperebbe in silenzio.
///
/// La scelta fra "filtra per semestre" e "filtra per anno/mese" si fa con una ricerca testuale sul
/// nome della tipologia:
///
///     contains("var") || contains("semestrale") || contains("annuale")  ->  FlagConguaglio
///     altrimenti                                                        ->  year + month
///
/// **`SEM. SOSPESI` non intercetta nessuno dei tre**, perché è abbreviato: finisce quindi nel ramo
/// anno/mese. Sembra un caso dimenticato, **non lo è** — le righe di SEM. SOSPESI conservano il
/// periodo di riferimento originale (v. `docs/business-fatturazione.md`). Normalizzare quella stringa
/// cambierebbe il contenuto dei report senza che nulla fallisca.
///
/// Gira sul DB seedato: il seed di `pfd.RelRighe` è costruito apposta per rendere la differenza
/// osservabile (stesso semestre, mesi diversi).
/// </summary>
public class RelRigheFiltroPeriodoIntegrationTests
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
    // L'invariante da proteggere
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task SemSospesi_ShouldFiltrarePerAnnoMese_NonPerSemestre()
    {
        // Il seed ha due righe SEM. SOSPESI con lo STESSO FlagConguaglio ('2026-S1') ma mesi diversi
        // (maggio e giugno). Chiedendo maggio deve tornare solo quella di maggio: se un domani la
        // tipologia finisse nel ramo del conguaglio, tornerebbero entrambe.
        var righe = await Righe("SEM. SOSPESI", 2026, 5);

        Assert.That(righe.Select(r => r.IdNotifica), Is.EquivalentTo(new[] { "REL-SS-MAG" }),
            "SEM. SOSPESI deve filtrare per anno/mese. Se compare anche REL-SS-GIU, qualcuno ha "
            + "'normalizzato' il confronto testuale su TipologiaFattura: v. docs/business-fatturazione.md, "
            + "è un comportamento voluto, non un caso dimenticato.");
    }

    [Test]
    public async Task VarSemestrale_ShouldFiltrarePerSemestre_IgnorandoIlMese()
    {
        // Stesso semestre, mesi diversi: entrambe devono uscire, perché "var"/"semestrale" fa scattare
        // il ramo FlagConguaglio. È il comportamento speculare al test precedente.
        var righe = await Righe("VAR. SEMESTRALE", 2026, 5);

        Assert.That(righe.Select(r => r.IdNotifica), Is.EquivalentTo(new[] { "REL-VS-MAG", "REL-VS-GIU" }),
            "VAR. SEMESTRALE deve ignorare il mese e prendere tutto il semestre.");
    }

    // ---------------------------------------------------------------------------------------------
    // L'altra regola non ovvia
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task PrimoSaldo_ShouldIncludereAncheLeRigheDiAsseverazione()
    {
        // Nel codice c'è un OR esplicito solo per il PRIMO SALDO:
        //   TipologiaFattura = @TipologiaFattura OR TipologiaFattura = 'ASSEVERAZIONE'
        // Coerente col fatto che l'asseverazione è gestita come filtro sulle stesse tabelle REL
        // (v. docs/pipeline-dati-send.md): il report del primo saldo deve contenerla.
        var righe = await Righe("PRIMO SALDO", 2026, 5);

        Assert.That(righe.Select(r => r.IdNotifica),
            Is.EquivalentTo(new[] { "REL-PS-1", "REL-PS-2", "REL-ASS-1" }),
            "Il PRIMO SALDO include le righe ASSEVERAZIONE dello stesso periodo.");
    }

    [Test]
    public async Task AltreTipologie_ShouldNonIncludereLAsseverazione()
    {
        // Contro-prova: l'OR vale SOLO per il primo saldo. SEM. SOSPESI è nello stesso periodo
        // dell'asseverazione, quindi se l'OR fosse applicato a tutti la vedremmo qui.
        var righe = await Righe("SEM. SOSPESI", 2026, 5);

        Assert.That(righe.Select(r => r.IdNotifica), Does.Not.Contain("REL-ASS-1"));
    }

    // ---------------------------------------------------------------------------------------------
    // Isolamento e input mancanti
    // ---------------------------------------------------------------------------------------------

    [Test]
    public void EnteDiversoDaQuelloDellaChiave_ShouldThrowDomainException()
    {
        // L'ente dell'identità e quello dentro la chiave devono coincidere: è il controllo che
        // impedisce di leggere il dettaglio notifiche di un altro aderente conoscendone la chiave.
        var chiave = $"22222222-2222-2222-2222-222222222222_{Contratto}_PRIMO-SALDO_2026_5";

        Assert.ThrowsAsync<DomainException>(async () =>
            await _handler.Send(new RelRigheQueryGetById(Auth(Ente)) { IdTestata = chiave }));
    }

    /// <summary>
    /// CARATTERIZZAZIONE di un difetto trovato scrivendo questi test.
    ///
    /// Prima di leggere le righe, `RelRigheQueryGetByIdHandler` cerca la **testata** del periodo e ne
    /// prende il `FlagConguaglio` con `FirstOrDefault()!` — senza controllo. Se la testata non esiste
    /// (periodo mai generato, o chiave sbagliata) il risultato non è una lista vuota: è una
    /// **NullReferenceException**.
    ///
    /// Non è teorico: questa query è chiamata da `CreateRelRighe`/`CreateRelSospese`, cioè dalla
    /// pipeline Synapse. Un periodo senza testata fa fallire la Function con un errore che non dice
    /// nulla, e il report semplicemente non viene generato — il sintomo che `pipeline-dati-send.md`
    /// descrive come "la causa è a monte, verificare l'esistenza del blob".
    ///
    /// È la stessa forma del 500 su `vwRelDettaglio` (v. Http/RelDettaglioHttpTests): dato mancante
    /// trattato come impossibile.
    ///
    /// Nota collegata: lo stesso handler **sovrascrive** il `FlagConguaglio` passato nella query con
    /// quello della testata — quindi valorizzarlo dall'esterno non ha effetto.
    /// </summary>
    [Test]
    public void PeriodoSenzaTestata_ShouldThrowNullReference_Caratterizzazione()
    {
        Assert.ThrowsAsync<NullReferenceException>(async () => await Righe("PRIMO SALDO", 1999, 1),
            "Comportamento attuale: nessuna testata -> NRE, non lista vuota.");
    }

    [Test]
    public async Task ContrattoDiverso_ShouldReturnVuoto()
    {
        // contract_id fa parte del WHERE: righe dello stesso ente su un altro contratto non escono.
        var chiave = $"{Ente}_CONTRATTO-INESISTENTE_PRIMO-SALDO_2026_5";
        var righe = await _handler.Send(new RelRigheQueryGetById(Auth(Ente)) { IdTestata = chiave });

        Assert.That(righe, Is.Empty);
    }

    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Non si passa `FlagConguaglio`: l'handler lo legge dalla testata del periodo e sovrascrive
    /// qualunque valore fornito dal chiamante. Il seed lo mette nelle testate 2026/5.
    /// </summary>
    private async Task<List<RigheRelDto>> Righe(string tipologia, int anno, int mese)
    {
        var chiave = $"{Ente}_{Contratto}_{tipologia.Replace(" ", "-")}_{anno}_{mese}";

        var righe = await _handler.Send(new RelRigheQueryGetById(Auth(Ente)) { IdTestata = chiave });

        return righe?.ToList() ?? [];
    }

    private static AuthenticationInfo Auth(string idEnte) => new()
    {
        Id = "integration-test-relrighe",
        IdEnte = idEnte,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };
}
