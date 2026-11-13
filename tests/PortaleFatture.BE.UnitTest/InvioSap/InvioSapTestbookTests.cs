using System.Reflection;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// Il testbook della pagina "Invio fatture a SAP" (PF-735/PF-965, casi T01–T51) a livello di unità:
/// per ogni caso, la regola del backend che non dipende dal DB — validazione dei filtri, modalità
/// della generazione (primo invio o re-invio), controlli sulla selezione, mapping di griglia e
/// dettaglio, formato della nota. Il nome di ogni test inizia con l'ID del caso.
///
/// L'esecuzione sulle rotte reali è in Http/InvioSapTestbookHttpTests (DB seedato), gli input ostili
/// in Http/InvioSapTestbookAdversarialHttpTests. I casi di sola interfaccia (T09–T12, T18, T24, T27,
/// T29, T36, T38, T39) non hanno test.
/// </summary>
[Category("InvioSap")]
[Category("TestbookInvioSap")]
public class InvioSapTestbookTests
{
    private const int Anno = 2026;
    private const int Mese = 1;
    private const string Tipologia = "PRIMO SALDO";
    private const string Utente = "admin@test";

    private static AuthenticationInfo Admin() => new() { Id = Utente, Prodotto = "prod-pn", Ruolo = Ruolo.ADMIN };

    private static InvioSapFatturaCorrente Fattura(long id, bool? inviata, int anno = Anno, int mese = Mese, string tipologia = Tipologia)
        => new(id, anno, mese, tipologia, inviata);

    private static InvioSapRichiestaGenerazione Richiesta(string? stato = null, bool? tutto = null, long[]? ids = null, long[]? escluse = null,
        string? nota = "Motivo", string? storno = null) => new()
        {
            Anno = Anno, Mese = Mese, TipologiaFattura = Tipologia, StatoInvio = stato, AllSelected = tutto,
            IdFatture = ids, IdFattureEscluse = escluse, Nota = nota, NumeroDocumentoStorno = storno
        };

    // ===================================================================================== filtri

    /// <summary>
    /// T04 — Filtro Anno: obbligatorio, dal 2000 all'anno successivo a quello corrente.
    /// </summary>
    [TestCase(null, false)]
    [TestCase(1999, false)]
    [TestCase(2000, true)]
    [TestCase(2026, true)]
    [TestCase(2027, true)]
    [TestCase(2028, false)]
    public void T04_FiltroAnno_ShouldEssereObbligatorioENellIntervallo(int? anno, bool valido)
        => Assert.That(InvioSapRegole.ValidaFiltriGriglia(anno, null, null, null, annoCorrente: 2026), valido ? Is.Empty : Is.Not.Empty);

    /// <summary>
    /// T05 — Mese "Tutti": nessun mese nella richiesta significa nessun filtro sul mese.
    /// </summary>
    [Test]
    public void T05_MeseTutti_ShouldDiventareNessunFiltroSulMese()
    {
        var senzaMesi = new InvioSapGrigliaRequest { Anno = 2003 }.Map(Admin());
        var listaVuota = new InvioSapGrigliaRequest { Anno = 2003, Mesi = [] }.Map(Admin());

        Assert.Multiple(() =>
        {
            Assert.That(senzaMesi.Mesi, Is.Null);
            Assert.That(listaVuota.Mesi, Is.Null);
        });
    }

    /// <summary>
    /// T06 — Filtro Mese: valori da 1 a 12, senza ripetizioni.
    /// </summary>
    [TestCase(new[] { 1, 12 }, true)]
    [TestCase(new[] { 0 }, false)]
    [TestCase(new[] { 13 }, false)]
    [TestCase(new[] { 3, 3 }, false)]
    public void T06_FiltroMese_ShouldAccettareSoloMesiValidiENonRipetuti(int[] mesi, bool valido)
        => Assert.That(InvioSapRegole.ValidaFiltriGriglia(2003, mesi, null, null, 2026), valido ? Is.Empty : Is.Not.Empty);

    /// <summary>
    /// T07 — Filtro Tipologia Fattura in multiselezione: le tipologie scelte arrivano alla query senza
    /// spazi attorno e senza doppioni; nessuna selezione significa tutte.
    /// </summary>
    [Test]
    public void T07_TipologieSelezionate_ShouldArrivareAllaQueryPulite()
    {
        var query = new InvioSapGrigliaRequest { Anno = 2003, TipologieFattura = [" ANTICIPO ", "PRIMO SALDO", "ANTICIPO"] }.Map(Admin());
        var nessuna = new InvioSapGrigliaRequest { Anno = 2003, TipologieFattura = [] }.Map(Admin());

        Assert.Multiple(() =>
        {
            Assert.That(query.TipologieFattura, Is.EqualTo(new[] { "ANTICIPO", "PRIMO SALDO" }));
            Assert.That(nessuna.TipologieFattura, Is.Null, "nessuna tipologia = tutte");
        });
    }

    /// <summary>
    /// T08, T16 — Stati del filtro: Tutte, Da inviare, Inviate, In elaborazione. "Re-inviate" non è uno
    /// stato (è un download a parte) e come filtro non è valido. Senza stato vale "Tutte".
    /// </summary>
    [Test]
    public void T08_T16_StatiDelFiltro_ShouldEssereQuattroESenzaReInviate()
    {
        var stati = InvioSapExtensions.Stati();

        Assert.Multiple(() =>
        {
            Assert.That(stati.Select(s => s.Codice), Is.EqualTo(new[] { "TUTTE", "DA_INVIARE", "INVIATE", "IN_ELABORAZIONE" }));
            Assert.That(stati.Select(s => s.Descrizione), Is.EqualTo(new[] { "Tutte", "Da inviare", "Inviate", "In elaborazione" }));
            Assert.That(FiltroStatoInvioSap.IsValido("REINVIATE"), Is.False);
            Assert.That(FiltroStatoInvioSap.Normalizza(null), Is.EqualTo("TUTTE"));
            Assert.That(FiltroStatoInvioSap.Normalizza(" inviate "), Is.EqualTo("INVIATE"));
        });
    }

    /// <summary>
    /// T16 — Lo stato di un rigo viene dal valore di FatturaInviata: 0 da inviare, 1 inviate, vuoto in
    /// elaborazione.
    /// </summary>
    [TestCase(false, "DA_INVIARE")]
    [TestCase(true, "INVIATE")]
    [TestCase(null, "IN_ELABORAZIONE")]
    public void T16_StatoDelRigo_ShouldDerivareDaFatturaInviata(bool? fatturaInviata, string atteso)
        => Assert.That(StatoInvioSap.DaFatturaInviata(fatturaInviata), Is.EqualTo(atteso));

    /// <summary>
    /// T17 — Combinazione di filtri: anno, mesi, tipologie e stato arrivano tutti alla query.
    /// </summary>
    [Test]
    public void T17_CombinazioneDiFiltri_ShouldArrivareInteraAllaQuery()
    {
        var query = new InvioSapGrigliaRequest { Anno = 2003, Mesi = [2], TipologieFattura = ["PRIMO SALDO"], StatoInvio = "inviate" }.Map(Admin());

        Assert.Multiple(() =>
        {
            Assert.That(query.Anno, Is.EqualTo(2003));
            Assert.That(query.Mesi, Is.EqualTo(new[] { 2 }));
            Assert.That(query.TipologieFattura, Is.EqualTo(new[] { "PRIMO SALDO" }));
            Assert.That(query.StatoInvio, Is.EqualTo("INVIATE"));
        });
    }

    // ==================================================================================== griglia

    /// <summary>
    /// T20 — Colonne della griglia: l'identificativo del rigo è tipologia, anno, mese e stato; l'importo
    /// è arrotondato a due decimali e un importo assente vale zero.
    /// </summary>
    [TestCase(211.004999, "211.00")]
    [TestCase(1832268.187, "1832268.19")]
    [TestCase(null, "0")]
    public void T20_RigoDellaGriglia_ShouldAvereIdEImportoADueDecimali(double? importo, string atteso)
    {
        var rigo = new[] { new InvioSapGrigliaDto { TipologiaFattura = "PRIMO SALDO", Anno = 2026, Mese = 5, StatoFattura = true, NumeroFatture = 128, ImportoTotale = importo } }.Map().Single();

        Assert.Multiple(() =>
        {
            Assert.That(rigo.Id, Is.EqualTo("PRIMO SALDO|2026|5|INVIATE"));
            Assert.That(rigo.NumeroFatture, Is.EqualTo(128));
            Assert.That(rigo.ImportoTotale, Is.EqualTo(decimal.Parse(atteso, System.Globalization.CultureInfo.InvariantCulture)));
        });
    }

    /// <summary>
    /// T20, T35 — Il mezzo centesimo esatto è arrotondato "al pari" (regola predefinita di
    /// <c>Math.Round</c>): 0,125 diventa 0,12 e 0,135 diventa 0,14. È la regola confermata il 13/11/2026
    /// per griglia e dettaglio. APP IO resta com'è (mezzo centesimo per eccesso).
    /// </summary>
    [TestCase(0.125, "0.12")]
    [TestCase(0.135, "0.14")]
    [TestCase(1832268.185, "1832268.18")]
    public void T20_T35_MezzoCentesimo_ShouldEssereArrotondatoAlPari_Caratterizzazione(double importo, string atteso)
    {
        var rigo = new[] { new InvioSapGrigliaDto { TipologiaFattura = "PRIMO SALDO", Anno = 2026, Mese = 5, StatoFattura = true, NumeroFatture = 1, ImportoTotale = importo } }.Map().Single();

        Assert.That(rigo.ImportoTotale, Is.EqualTo(decimal.Parse(atteso, System.Globalization.CultureInfo.InvariantCulture)));
    }

    // ========================================================================== dettaglio fatture

    private static InvioSapDettaglioDto FatturaDto(string? note, string? nomeFile, bool? inviata, DateTime? generazioneUtc = null) => new()
    {
        IdFattura = 9502, FkIdTipoDocumento = "TD01", FkTipologiaFattura = "PRIMO SALDO", FkIdEnte = "ente-1", RagioneSociale = "Comune di Prova",
        DataFattura = new DateTime(2026, 2, 10), IdentificativoFattura = "9502/2026", TotaleFattura = 105.504999, AnnoRiferimento = 2026, MeseRiferimento = 1,
        IdDocumento = "  ", Progressivo = 42, FatturaInviata = inviata, DataUltimaGenerazione = generazioneUtc, NomeFile = nomeFile, NoteInvio = note
    };

    private static string Note(params InvioSapNota[] note) => "[" + string.Join(",", note.Select(n => "{\"Nota\":" + n.ToJson() + "}")) + "]";

    /// <summary>
    /// T22 — Dettaglio Fatture: Ragione Sociale, ID Fattura, Data Fattura, Importo e File JSON di ogni
    /// fattura del rigo, con count separato dall'elenco.
    /// </summary>
    [Test]
    public void T22_FatturaDelDettaglio_ShouldPortareIDatiMostratiDallaPagina()
    {
        var risposta = new InvioSapDettaglioListDto { Count = 7, Fatture = [FatturaDto(null, "FatturePagoPa.json", true)] }.Map();
        var f = risposta.Fatture.Single();

        Assert.Multiple(() =>
        {
            Assert.That(risposta.Count, Is.EqualTo(7));
            Assert.That(f.RagioneSociale, Is.EqualTo("Comune di Prova"));
            Assert.That(f.IdFattura, Is.EqualTo(9502));
            Assert.That(f.NumeroFattura, Is.EqualTo("9502/2026"));
            Assert.That(f.DataFattura, Is.EqualTo(new DateTime(2026, 2, 10)));
            Assert.That(f.NomeFile, Is.EqualTo("FatturePagoPa.json"));
            Assert.That(f.StatoInvio, Is.EqualTo("INVIATE"));
            Assert.That(f.IdDocumento, Is.Null, "un id documento di soli spazi non è un valore");
        });
    }

    /// <summary>
    /// T23, T25 — Filtri e paginazione del dettaglio: il rigo va indicato per intero (anno, mese,
    /// tipologia, stato); la pagina parte da 1, la dimensione va da 1 a 500, e i due valori vanno
    /// insieme. Pagina 0 non è valida.
    /// </summary>
    [TestCase(null, null, true)]
    [TestCase(1, 10, true)]
    [TestCase(1, 500, true)]
    [TestCase(0, 10, false)]
    [TestCase(-1, 10, false)]
    [TestCase(1, 0, false)]
    [TestCase(1, 501, false)]
    [TestCase(1, null, false)]
    [TestCase(null, 10, false)]
    public void T23_T25_PaginazioneDelDettaglio_ShouldPartireDaUno(int? page, int? pageSize, bool valida)
    {
        var errori = InvioSapRegole.ValidaFiltriDettaglio(2003, 1, Tipologia, "INVIATE", null, null, page, pageSize);

        Assert.That(errori, valida ? Is.Empty : Is.EqualTo(new[] { InvioSapRegole.ErrorePaginazione }));
    }

    /// <summary>
    /// T22 — Il dettaglio si apre per un rigo preciso: lo stato "Tutte" o assente non lo identifica.
    /// </summary>
    [TestCase("DA_INVIARE", true)]
    [TestCase("INVIATE", true)]
    [TestCase("IN_ELABORAZIONE", true)]
    [TestCase("TUTTE", false)]
    [TestCase(null, false)]
    [TestCase("REINVIATE", false)]
    public void T22_StatoDelRigoNelDettaglio_ShouldEssereObbligatorio(string? stato, bool valido)
    {
        var errori = InvioSapRegole.ValidaFiltriDettaglio(2003, 1, Tipologia, stato, null, null, null, null);

        Assert.That(errori, valido ? Is.Empty : Is.EqualTo(new[] { InvioSapRegole.ErroreStatoDettaglio }));
    }

    /// <summary>
    /// T33 — Date Invio: l'elenco degli invii di una fattura, numerati, con data in ora italiana e
    /// nota. Una fattura mai inviata non ne ha.
    /// </summary>
    [Test]
    public void T33_DateInvio_ShouldEssereNumerateInOraItaliana()
    {
        var note = Note(new InvioSapNota(new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc), "Primo invio", "GeneraJsonFatture"),
                        new InvioSapNota(new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc), "Re-invio", "GeneraJsonFatture"));

        var inviata = new InvioSapDettaglioListDto { Count = 1, Fatture = [FatturaDto(note, "f.json", true)] }.Map().Fatture.Single();
        var maiInviata = new InvioSapDettaglioListDto { Count = 1, Fatture = [FatturaDto(null, null, false)] }.Map().Fatture.Single();

        Assert.Multiple(() =>
        {
            Assert.That(inviata.NumeroInvii, Is.EqualTo(2));
            Assert.That(inviata.StoricoInvii.Select(s => s.NInvio), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(inviata.StoricoInvii.Select(s => s.Testo), Is.EqualTo(new[] { "Primo invio", "Re-invio" }));
            Assert.That(inviata.StoricoInvii[0].Data, Is.EqualTo(new DateTime(2026, 7, 1, 12, 0, 0)), "ora legale: UTC+2");
            Assert.That(inviata.StoricoInvii[1].Data, Is.EqualTo(new DateTime(2026, 12, 1, 11, 0, 0)), "ora solare: UTC+1");
            Assert.That(maiInviata.NumeroInvii, Is.EqualTo(0));
            Assert.That(maiInviata.StoricoInvii, Is.Empty);
        });
    }

    /// <summary>
    /// T34 — Nome del file JSON: presente per le fatture inviate, assente per quelle da inviare, senza
    /// valori inventati dal backend.
    /// </summary>
    [Test]
    public void T34_NomeFileJson_ShouldEssereAssentePerLeFattureDaInviare()
    {
        var daInviare = new InvioSapDettaglioListDto { Count = 1, Fatture = [FatturaDto(null, null, false)] }.Map().Fatture.Single();

        Assert.Multiple(() =>
        {
            Assert.That(daInviare.NomeFile, Is.Null);
            Assert.That(daInviare.DataUltimaGenerazione, Is.Null);
            Assert.That(daInviare.StatoInvio, Is.EqualTo("DA_INVIARE"));
        });
    }

    /// <summary>
    /// T35 — Importi e date del dettaglio: l'importo è arrotondato a due decimali, la data della fattura
    /// non viene spostata, la data di generazione è portata in ora italiana.
    /// </summary>
    [Test]
    public void T35_ImportiEDate_ShouldEssereArrotondatiEInOraItaliana()
    {
        var f = new InvioSapDettaglioListDto { Count = 1, Fatture = [FatturaDto(null, "f.json", true, new DateTime(2026, 7, 1, 22, 30, 0, DateTimeKind.Utc))] }.Map().Fatture.Single();

        Assert.Multiple(() =>
        {
            Assert.That(f.Importo, Is.EqualTo(105.50m));
            Assert.That(f.DataFattura, Is.EqualTo(new DateTime(2026, 2, 10)));
            Assert.That(f.DataUltimaGenerazione, Is.EqualTo(new DateTime(2026, 7, 2, 0, 30, 0)), "22:30 UTC = 00:30 del giorno dopo in Italia");
        });
    }

    // ============================================================= selezione e generazione del JSON

    /// <summary>
    /// T26, T28, T30 — Le tre forme di selezione: rigo intero (checkbox del rigo), singole fatture,
    /// rigo intero meno le deselezionate. Con lo stato del rigo aperto la modalità è primo invio per
    /// "da inviare" e re-invio per "inviate".
    /// </summary>
    [TestCase("DA_INVIARE", InvioSapModalita.PrimoInvio)]
    [TestCase("INVIATE", InvioSapModalita.ReInvio)]
    [TestCase(" inviate ", InvioSapModalita.ReInvio)]
    public void T26_T28_T30_ModalitaDellaGenerazione_ShouldDipendereDalloStatoDelRigo(string stato, InvioSapModalita attesa)
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.Modalita(Richiesta(stato, tutto: true)), Is.EqualTo(attesa), "rigo intero");
            Assert.That(InvioSapRegole.Modalita(Richiesta(stato, ids: [1])), Is.EqualTo(attesa), "singole fatture");
            Assert.That(InvioSapRegole.Modalita(Richiesta(stato, tutto: true, escluse: [2])), Is.EqualTo(attesa), "rigo meno le deselezionate");
            Assert.That(InvioSapRegole.SelezioneTotale(Richiesta(stato, tutto: true)), Is.True);
            Assert.That(InvioSapRegole.SelezioneTotale(Richiesta(stato, ids: [1])), Is.False);
        });
    }

    /// <summary>
    /// T28 — Singole fatture: l'elenco non può essere vuoto, contenere id non validi o ripetuti, né
    /// superare le mille fatture.
    /// </summary>
    [Test]
    public void T28_ElencoDelleFatture_ShouldEssereValido()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.ValidaFormale(Richiesta("DA_INVIARE", ids: [9505, 9506]), Utente), Is.Empty);
            Assert.That(InvioSapRegole.ValidaFormale(Richiesta("DA_INVIARE", ids: []), Utente), Does.Contain(InvioSapRegole.ErroreSelezioneVuota));
            Assert.That(InvioSapRegole.ValidaFormale(Richiesta("DA_INVIARE", ids: [0]), Utente), Does.Contain(InvioSapRegole.ErroreIdNonValido));
            Assert.That(InvioSapRegole.ValidaFormale(Richiesta("DA_INVIARE", ids: [9505, 9505]), Utente), Does.Contain(InvioSapRegole.ErroreSelezioneDuplicata));
            Assert.That(InvioSapRegole.ValidaFormale(Richiesta("DA_INVIARE", ids: Enumerable.Range(1, 1001).Select(i => (long)i).ToArray()), Utente), Does.Contain(InvioSapRegole.ErroreSelezioneTroppoGrande));
        });
    }

    /// <summary>
    /// T30 — Fatture deselezionate: devono esistere e appartenere al rigo. Una deselezionata sbagliata
    /// non viene ignorata, perché vorrebbe dire inviare una fattura che l'utente aveva tolto.
    /// </summary>
    [Test]
    public void T30_FattureDeselezionate_ShouldAppartenereAlRigo()
    {
        InvioSapFatturaCorrente[] correnti = [Fattura(1, true), Fattura(2, true), Fattura(3, true, mese: 7)];

        var valida = InvioSapRegole.ValidaEsclusioni(Richiesta("INVIATE", tutto: true, escluse: [2], storno: "NC-1"), correnti);
        var diUnAltroRigo = InvioSapRegole.ValidaEsclusioni(Richiesta("INVIATE", tutto: true, escluse: [3], storno: "NC-1"), correnti);
        var inesistente = InvioSapRegole.ValidaEsclusioni(Richiesta("INVIATE", tutto: true, escluse: [99], storno: "NC-1"), correnti);

        Assert.Multiple(() =>
        {
            Assert.That(valida, Is.Null);
            Assert.That(diUnAltroRigo!.Errori, Is.EqualTo(new[] { InvioSapRegole.ErroreFuoriGruppo }));
            Assert.That(inesistente!.Tipo, Is.EqualTo(InvioSapGeneraEsitoTipo.NonTrovate));
        });
    }

    /// <summary>
    /// T31 — Un solo gruppo alla volta: una selezione con una fattura di un'altra tipologia, di un altro
    /// mese o di un altro anno viene rifiutata, indicando le fatture fuori posto.
    /// </summary>
    [Test]
    public void T31_FattureDiUnAltroGruppo_ShouldEssereRifiutate()
    {
        InvioSapFatturaCorrente[] correnti =
        [
            Fattura(1, false), Fattura(2, false, tipologia: "SECONDO SALDO"), Fattura(3, false, mese: 2), Fattura(4, false, anno: 2025)
        ];

        var stessoGruppo = InvioSapRegole.ValidaSelezione(Richiesta("DA_INVIARE", ids: [1]), correnti);
        var misti = InvioSapRegole.ValidaSelezione(Richiesta("DA_INVIARE", ids: [1, 2, 3, 4]), correnti);

        Assert.Multiple(() =>
        {
            Assert.That(stessoGruppo, Is.Null);
            Assert.That(misti!.Errori, Is.EqualTo(new[] { InvioSapRegole.ErroreFuoriGruppo }));
            Assert.That(misti.IdFattureCoinvolte, Is.EqualTo(new long[] { 2, 3, 4 }));
        });
    }

    /// <summary>
    /// T32 — Una fattura in elaborazione non è selezionabile: non va bene né per il primo invio né per
    /// il re-invio.
    /// </summary>
    [Test]
    public void T32_FatturaInElaborazione_ShouldNonEssereSelezionabile()
    {
        InvioSapFatturaCorrente[] correnti = [Fattura(1, null)];

        var primoInvio = InvioSapRegole.ValidaSelezione(Richiesta("DA_INVIARE", ids: [1]), correnti);
        var reInvio = InvioSapRegole.ValidaSelezione(Richiesta("INVIATE", ids: [1], storno: "NC-1"), correnti);

        Assert.Multiple(() =>
        {
            Assert.That(primoInvio!.Errori, Is.EqualTo(new[] { InvioSapRegole.ErroreNonDaInviare }));
            Assert.That(reInvio!.Errori, Is.EqualTo(new[] { InvioSapRegole.ErroreNonInviate }));
        });
    }

    /// <summary>
    /// T37, T47 — Fattura inviata: il re-invio pretende Numero Documento Storno e nota. Nel primo invio
    /// la nota resta obbligatoria e lo storno non è ammesso.
    /// </summary>
    [Test]
    public void T37_T47_ReInvio_ShouldRichiedereStornoENota()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.ValidaFormale(Richiesta("INVIATE", ids: [1], storno: "NC-1"), Utente), Is.Empty);
            Assert.That(InvioSapRegole.ValidaFormale(Richiesta("INVIATE", ids: [1], storno: null), Utente), Is.EqualTo(new[] { InvioSapRegole.ErroreStornoMancante }));
            Assert.That(InvioSapRegole.ValidaFormale(Richiesta("INVIATE", ids: [1], storno: "  "), Utente), Is.EqualTo(new[] { InvioSapRegole.ErroreStornoMancante }));
            Assert.That(InvioSapRegole.ValidaFormale(Richiesta("INVIATE", ids: [1], storno: "NC-1", nota: null), Utente), Is.EqualTo(new[] { InvioSapRegole.ErroreNota }));
            Assert.That(InvioSapRegole.ValidaFormale(Richiesta("DA_INVIARE", ids: [1], nota: " "), Utente), Is.EqualTo(new[] { InvioSapRegole.ErroreNota }));
            Assert.That(InvioSapRegole.ValidaFormale(Richiesta("DA_INVIARE", ids: [1], storno: "NC-1"), Utente), Is.EqualTo(new[] { InvioSapRegole.ErroreStornoNonAmmesso }));
        });
    }

    /// <summary>
    /// T47 — Dopo un invio riuscito la fattura è "inviata": si può re-inviare, ma non rimandarla come
    /// primo invio.
    /// </summary>
    [Test]
    public void T47_FatturaGiaInviata_ShouldEssereSoloReInviabile()
    {
        InvioSapFatturaCorrente[] correnti = [Fattura(1, true)];

        Assert.Multiple(() =>
        {
            Assert.That(InvioSapRegole.ValidaSelezione(Richiesta("INVIATE", ids: [1], storno: "NC-1"), correnti), Is.Null);
            Assert.That(InvioSapRegole.ValidaSelezione(Richiesta("DA_INVIARE", ids: [1]), correnti)!.Errori, Is.EqualTo(new[] { InvioSapRegole.ErroreNonDaInviare }));
        });
    }

    /// <summary>
    /// T44, T49 — Nota con accenti, apostrofi, virgolette, HTML e caratteri di controllo: scritta nel
    /// JSON della nota e riletta dall'elenco degli invii torna identica.
    /// </summary>
    [TestCase("Perché l'ente ha chiesto \"rettifica\" — già così: 100% & à è ì ò ù €")]
    [TestCase("<script>alert('xss')</script><img src=x onerror=alert(1)>")]
    [TestCase("riga 1\nriga 2\ttab \\ barra } ] {")]
    [TestCase("'; DROP TABLE pfw.DettaglioInvioSAP; --")]
    [TestCase("emoji 🚀 e 漢字")]
    public void T44_T49_NotaConCaratteriSpeciali_ShouldTornareIdentica(string testo)
    {
        var json = Note(new InvioSapNota(new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc), testo, InvioSapRegole.DescrizioneAzione));

        var lette = InvioSapNota.ParseNoteInvio(json);

        Assert.Multiple(() =>
        {
            Assert.That(lette, Has.Count.EqualTo(1));
            Assert.That(lette[0].Testo, Is.EqualTo(testo));
            Assert.That(() => System.Text.Json.JsonDocument.Parse(json), Throws.Nothing, "il JSON resta valido");
        });
    }

    /// <summary>
    /// T44 — La nota ha un limite di 2000 caratteri, lo storno di 255.
    /// </summary>
    [TestCase(2000, 255, true)]
    [TestCase(2001, 255, false)]
    [TestCase(2000, 256, false)]
    public void T44_LunghezzaDiNotaEStorno_ShouldAvereUnLimite(int nota, int storno, bool valida)
    {
        var errori = InvioSapRegole.ValidaFormale(Richiesta("INVIATE", ids: [1], nota: new string('n', nota), storno: new string('s', storno)), Utente);

        Assert.That(errori, valida ? Is.Empty : Is.Not.Empty);
    }

    /// <summary>
    /// T51 — I messaggi di errore che il backend manda alla pagina sono frasi in italiano, non codici né
    /// termini tecnici.
    /// </summary>
    [Test]
    public void T51_MessaggiDiErrore_ShouldEssereFrasiComprensibili()
    {
        var messaggi = typeof(InvioSapRegole).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.Name.StartsWith("Errore"))
            .Select(f => (f.Name, Testo: (string)f.GetRawConstantValue()!))
            .ToList();

        Assert.That(messaggi, Has.Count.GreaterThan(15));
        foreach (var (nome, testo) in messaggi)
            Assert.Multiple(() =>
            {
                Assert.That(testo, Has.Length.GreaterThan(10), nome);
                Assert.That(char.IsUpper(testo[0]), Is.True, nome);
                Assert.That(testo, Does.Contain(" "), nome + ": una frase, non un codice");
                Assert.That(testo, Does.Not.Contain("Exception").And.Not.Contain("null").And.Not.Contain("_ERR"), nome);
            });
    }
}
