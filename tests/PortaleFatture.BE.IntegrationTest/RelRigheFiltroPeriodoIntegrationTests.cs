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
/// Il motivo per cui vale più di una copertura qualsiasi: la scelta fra "filtra per semestre" e
/// "filtra per anno/mese" è un ramo invisibile — nessun errore, solo righe in più o in meno nel
/// report — ed è esattamente il tipo di cosa che una modifica ben intenzionata sposta in silenzio.
///
/// Regola in vigore (dal 07/09/2026):
///
///     TipologiaFattura == "VAR. SEMESTRALE"  ->  FlagConguaglio (tutto il semestre)
///     qualunque altra                        ->  year + month
///
/// Prima il ramo del conguaglio era scelto con una ricerca testuale
/// — contains("var") || contains("semestrale") || contains("annuale") — che catturava anche
/// **`VAR. ANNUALE`**, e che invece **non** catturava `SEM. SOSPESI` perché abbreviato. Delle due
/// asimmetrie ne resta una sola, ed è ora esplicita: SEM. SOSPESI filtra per anno/mese non "per
/// distrazione dell'abbreviazione" ma perché è la regola generale, coerente col fatto che le sue
/// righe conservano il periodo di riferimento originale (v. `docs/business-fatturazione.md`).
///
/// Gira sul DB seedato: le righe di `pfd.RelRighe` sono costruite apposta per rendere la differenza
/// osservabile — per SEM. SOSPESI, VAR. SEMESTRALE e VAR. ANNUALE il seed ha la **stessa** coppia
/// maggio/giugno con lo **stesso** `FlagConguaglio '2026-S1'`, quindi a parità di dati l'unica cosa
/// che può cambiare il risultato è il ramo scelto dal codice.
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
            "SEM. SOSPESI deve filtrare per anno/mese: le sue righe conservano il periodo di "
            + "riferimento originale (v. docs/business-fatturazione.md). Se compare anche REL-SS-GIU, "
            + "qualcuno l'ha spostata nel ramo del conguaglio.");
    }

    /// <summary>
    /// REGRESSIONE della modifica del 07/09/2026: `VAR. ANNUALE` è passata dal ramo semestre a quello
    /// anno/mese, ed è l'**unica** tipologia il cui comportamento è cambiato.
    ///
    /// Il seed le dà la stessa forma di VAR. SEMESTRALE — due righe, maggio e giugno, stesso
    /// `FlagConguaglio '2026-S1'` — proprio perché le due tipologie erano indistinguibili per il
    /// vecchio confronto testuale (`contains("annuale")` e `contains("var")` scattavano entrambi).
    /// Con quel codice questo test tornerebbe due righe e sarebbe rosso: è la sua ragione d'essere.
    /// </summary>
    [Test]
    public async Task VarAnnuale_ShouldFiltrarePerAnnoMese_NonPerSemestre()
    {
        var righe = await Righe("VAR. ANNUALE", 2026, 5);

        Assert.That(righe.Select(r => r.IdNotifica), Is.EquivalentTo(new[] { "REL-VA-MAG" }),
            "VAR. ANNUALE deve filtrare per anno/mese. Se compare anche REL-VA-GIU, il ramo del "
            + "conguaglio è tornato a catturarla — tipicamente reintroducendo la ricerca testuale "
            + "contains(\"var\"|\"semestrale\"|\"annuale\") al posto del confronto con "
            + "TipologiaFattura.VAR_SEMESTRALE.");
    }

    [Test]
    public async Task VarSemestrale_ShouldFiltrarePerSemestre_IgnorandoIlMese()
    {
        // L'UNICA tipologia che filtra per semestre. Stesso seed dei due test sopra (maggio + giugno,
        // stesso FlagConguaglio): qui però devono uscire entrambe. Il contrasto a parità di dati è
        // ciò che rende il ramo osservabile.
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
