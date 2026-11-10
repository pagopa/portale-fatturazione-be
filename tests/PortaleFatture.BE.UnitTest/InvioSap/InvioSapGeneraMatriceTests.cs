using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// Matrice completa delle regole formali del Genera (be-invio-sap.md §3.3): tutte le combinazioni di
/// <c>allSelected</c> (true, false, assente) × <c>idFatture</c> × <c>idFattureEscluse</c> × storno, 216 casi.
/// L'atteso non riusa il codice sotto test: e' la regola scritta come tabella.
///   - quale lista conta: con <c>allSelected = true</c> le escluse, altrimenti <c>idFatture</c>; l'altra non viene letta;
///   - modalita': primo invio solo senza seleziona tutto e senza <c>idFatture</c>; in ogni altro caso re-invio;
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

    public static IEnumerable<TestCaseData> Combinazioni() =>
        from s in Selezioni
        from ids in Liste
        from escluse in Liste
        from storno in Storni
        select new TestCaseData(s.Valore, ids.Nome, escluse.Nome, storno.Valore)
            .SetName($"Matrice_Formale(allSelected {s.Nome}; idFatture {ids.Nome}; escluse {escluse.Nome}; {storno.Nome})");

    private static long[]? Lista(string nome) => Liste.Single(l => l.Nome == nome).Valore;

    [TestCaseSource(nameof(Combinazioni))]
    public void Matrice_Formale(bool? allSelected, string formaIds, string formaEscluse, string? storno)
    {
        var richiesta = new InvioSapRichiestaGenerazione
        {
            Anno = 2093, Mese = 1, TipologiaFattura = "PRIMO SALDO", Nota = "Matrice",
            AllSelected = allSelected, IdFatture = Lista(formaIds), IdFattureEscluse = Lista(formaEscluse),
            NumeroDocumentoStorno = storno
        };

        // ---- Atteso, dalla regola ----
        var tutto = allSelected == true;
        var primoInvio = !tutto && formaIds == "assente";
        var attesi = new List<string>();
        if (primoInvio)
        {
            if (storno is not null) attesi.Add(InvioSapRegole.ErroreStornoNonAmmesso);
        }
        else
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
                    "vuota" => InvioSapRegole.ErroreSelezioneVuota,
                    "con id zero" => InvioSapRegole.ErroreIdNonValido,
                    "con ripetizioni" => InvioSapRegole.ErroreSelezioneDuplicata,
                    "oltre il tetto" => InvioSapRegole.ErroreSelezioneTroppoGrande,
                    _ => null
                };
            if (erroreLista is not null) attesi.Add(erroreLista);
            if (storno is null) attesi.Add(InvioSapRegole.ErroreStornoMancante);
        }

        // ---- Verifica ----
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.Modalita(richiesta),
                Is.EqualTo(primoInvio ? InvioSapModalita.PrimoInvio : InvioSapModalita.ReInvio), "Modalita'");
            Assert.That(InvioSapRegole.SelezioneTotale(richiesta), Is.EqualTo(tutto), "Seleziona tutto");
            Assert.That(InvioSapRegole.ValidaFormale(richiesta, Utente), Is.EquivalentTo(attesi), "Errori formali");
        });
    }

    [Test]
    public void Matrice_ShouldCoprireTutteLeCombinazioni()
    {
        Assert.That(Combinazioni().Count(), Is.EqualTo(3 * 6 * 6 * 2));
    }

    [Test]
    public void SelezionaTutto_ShouldNonEssereMaiUnPrimoInvio()
    {
        // La garanzia piu' importante: nessuna combinazione con allSelected = true puo' inviare fatture mai inviate.
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
