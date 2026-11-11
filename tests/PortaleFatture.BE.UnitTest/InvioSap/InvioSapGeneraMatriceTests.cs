using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// Matrice completa delle regole formali del Genera (be-invio-sap.md §3.3): tutte le combinazioni di
/// <c>statoInvio</c> (assente, DA_INVIARE, INVIATE, non valido) × <c>allSelected</c> (true, false, assente) ×
/// <c>idFatture</c> × <c>idFattureEscluse</c> × storno, 864 casi.
/// L'atteso non riusa il codice sotto test: e' la regola scritta come tabella.
///   - quale lista conta: con <c>allSelected = true</c> le escluse, altrimenti <c>idFatture</c>; l'altra non viene letta;
///   - modalita': con <c>statoInvio</c> DA_INVIARE primo invio, con INVIATE re-invio; senza (o non valido) primo
///     invio solo senza seleziona tutto e senza <c>idFatture</c>, in ogni altro caso re-invio;
///   - selezione: nel primo invio senza seleziona tutto e senza <c>idFatture</c> parte il rigo intero;
///   - storno: vietato nel primo invio, obbligatorio nel re-invio.
/// Serve a garantire che nessuna combinazione cambi modalita' per effetto di un campo che non dovrebbe contare.
/// </summary>
[Category("InvioSap")]
public class InvioSapGeneraMatriceTests
{
    private const string Utente = "admin@test";

    private static readonly long[] OltreIlTetto = Enumerable.Range(1, InvioSapRegole.MaxFatture + 1).Select(i => (long)i).ToArray();

    /// <summary>Forme di una lista di id e l'errore che darebbe se fosse la lista che conta (null = valida).</summary>
    private static readonly (string Nome, long[]? Valore)[] Liste =
    [
        ("assente", null),
        ("vuota", []),
        ("valida", [9502, 9503]),
        ("con id zero", [0]),
        ("con ripetizioni", [9502, 9502]),
        ("oltre il tetto", OltreIlTetto)
    ];

    private static readonly (string Nome, string? Valore)[] Storni = [("senza storno", null), ("con storno", "NC-1")];
    private static readonly (string Nome, bool? Valore)[] Selezioni = [("true", true), ("false", false), ("assente", null)];
    private static readonly (string Nome, string? Valore)[] Stati =
        [("assente", null), ("DA_INVIARE", "DA_INVIARE"), ("INVIATE", " inviate "), ("non valido", "REINVIATE")];

    public static IEnumerable<TestCaseData> Combinazioni() =>
        from stato in Stati
        from s in Selezioni
        from ids in Liste
        from escluse in Liste
        from storno in Storni
        select new TestCaseData(stato.Nome, s.Valore, ids.Nome, escluse.Nome, storno.Valore)
            .SetName($"Matrice_Formale(statoInvio {stato.Nome}; allSelected {s.Nome}; idFatture {ids.Nome}; escluse {escluse.Nome}; {storno.Nome})");

    private static long[]? Lista(string nome) => Liste.Single(l => l.Nome == nome).Valore;

    [TestCaseSource(nameof(Combinazioni))]
    public void Matrice_Formale(string formaStato, bool? allSelected, string formaIds, string formaEscluse, string? storno)
    {
        var richiesta = new InvioSapRichiestaGenerazione
        {
            Anno = 2093, Mese = 1, TipologiaFattura = "PRIMO SALDO", Nota = "Matrice",
            StatoInvio = Stati.Single(x => x.Nome == formaStato).Valore,
            AllSelected = allSelected, IdFatture = Lista(formaIds), IdFattureEscluse = Lista(formaEscluse),
            NumeroDocumentoStorno = storno
        };

        // ---- Atteso, dalla regola ----
        var tutto = allSelected == true;
        var primoInvio = formaStato switch
        {
            "DA_INVIARE" => true,
            "INVIATE" => false,
            _ => !tutto && formaIds == "assente"
        };
        var rigoIntero = primoInvio && !tutto && formaIds == "assente";
        var attesi = new List<string>();
        if (formaStato == "non valido") attesi.Add(InvioSapRegole.ErroreStatoGenera);
        if (!rigoIntero)
        {
            var erroreLista = tutto
                ? formaEscluse switch
                {
                    "con id zero" => InvioSapRegole.ErroreIdNonValido,
                    "con ripetizioni" => InvioSapRegole.ErroreEsclusioniDuplicate,
                    "oltre il tetto" => InvioSapRegole.ErroreEsclusioniTroppe,
                    _ => null // assente, vuota, valida: nessuna esclusa e' ammesso
                }
                : formaIds switch
                {
                    "assente" or "vuota" => InvioSapRegole.ErroreSelezioneVuota,
                    "con id zero" => InvioSapRegole.ErroreIdNonValido,
                    "con ripetizioni" => InvioSapRegole.ErroreSelezioneDuplicata,
                    "oltre il tetto" => InvioSapRegole.ErroreSelezioneTroppoGrande,
                    _ => null
                };
            if (erroreLista is not null) attesi.Add(erroreLista);
        }
        if (primoInvio)
        {
            if (storno is not null) attesi.Add(InvioSapRegole.ErroreStornoNonAmmesso);
        }
        else if (storno is null) attesi.Add(InvioSapRegole.ErroreStornoMancante);

        // ---- Verifica ----
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.Modalita(richiesta),
                Is.EqualTo(primoInvio ? InvioSapModalita.PrimoInvio : InvioSapModalita.ReInvio), "Modalita'");
            Assert.That(InvioSapRegole.SelezioneTotale(richiesta), Is.EqualTo(tutto), "Seleziona tutto");
            Assert.That(InvioSapRegole.RigoIntero(richiesta), Is.EqualTo(rigoIntero), "Rigo intero");
            Assert.That(InvioSapRegole.ValidaFormale(richiesta, Utente), Is.EquivalentTo(attesi), "Errori formali");
        });
    }

    [Test]
    public void Matrice_ShouldCoprireTutteLeCombinazioni()
    {
        Assert.That(Combinazioni().Count(), Is.EqualTo(4 * 3 * 6 * 6 * 2));
    }

    [Test]
    public void SelezionaTutto_SenzaStatoDaInviare_ShouldNonEssereMaiUnPrimoInvio()
    {
        // La garanzia piu' importante: con allSelected = true le fatture mai inviate partono solo se il body dice
        // esplicitamente DA_INVIARE. Senza statoInvio nessuna combinazione diventa un primo invio.
        var primiInvii = from ids in Liste
                         from escluse in Liste
                         from storno in Storni
                         let r = new InvioSapRichiestaGenerazione
                         {
                             Anno = 2093, Mese = 1, TipologiaFattura = "PRIMO SALDO", Nota = "x", AllSelected = true,
                             IdFatture = ids.Valore, IdFattureEscluse = escluse.Valore, NumeroDocumentoStorno = storno.Valore
                         }
                         where InvioSapRegole.Modalita(r) == InvioSapModalita.PrimoInvio
                         select r;
        Assert.That(primiInvii, Is.Empty);
    }
}
