using System.Globalization;
using PortaleFatture.BE.Core.Entities.SEND.DatiFatturazioni;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;
using PortaleFatture.BE.Core.Entities.SEND.Fatture;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Unit test di <see cref="DocumentBuilder"/> (documenti SEND: REL, PEC, modulo commessa, dettaglio
/// dei documenti contabili sospesi/emessi). Nessun DB.
///
/// Due famiglie di test:
///  - **scelta del template**: una cartella temporanea con un template finto per ogni file, il cui
///    testo dice il proprio nome ("TEMPLATE:&lt;nome&gt;"). Cosi' si verifica il ramo senza dipendere
///    dal contenuto dei template veri;
///  - **template veri dall'output della build**: il builder punta alla cartella di output dei test,
///    dove i template arrivano per <c>CopyToOutputDirectory</c> dei progetti referenziati (Api e
///    SendEmailFunction). E' la stessa strada del runtime (ContentRootPath / CurrentDirectory): se
///    un template non viene copiato — la causa del difetto PF-997 su SEM. SOSPESI — qui diventa
///    rosso, invece di scoprirlo al primo download in produzione.
///
/// I test con suffisso <c>_Caratterizzazione</c> fissano un comportamento attuale discutibile, non
/// quello desiderato: se qualcuno lo cambia, il test diventa rosso e va aggiornato consapevolmente.
/// </summary>
public class DocumentBuilderTests
{
    private const string Documenti = "Infrastructure/Documenti/";

    private static readonly string[] TemplateNomi =
    [
        "primo_saldo.html", "secondo_saldo.html", "var_semestrale.html",
        "PRIMO_SALDO_rel.html", "SECONDO_SALDO_rel.html", "SEM_SOSPESI_rel.html",
        "daticommessa.html",
        "pac_fattura_sospesa_singola V2.html", "pal_fattura_sospesa_singola V2.html",
        "pac_fattura_emessa_singola V2.html", "pal_fattura_emessa_singola V2.html",
        "pac_fatture_emesse_multiple V2.html", "pal_fatture_emesse_multiple V2.html",
        "pac_pec_sospesa.html", "pac_pec_emessa_singola.html", "pac_pec_emesse_multiple.html",
        "pal_pec_sospesa.html", "pal_pec_emessa_singola.html", "pal_pec_emesse_multiple.html",
    ];

    private string _rootFinta = null!;

    [OneTimeSetUp]
    public void CreaTemplateFinti()
    {
        _rootFinta = Path.Combine(Path.GetTempPath(), "pfw-tests", "docbuilder-" + Guid.NewGuid().ToString("N"));
        var dir = Path.Combine(_rootFinta, Documenti);
        Directory.CreateDirectory(dir);

        // Ogni template finto dichiara il proprio nome e contiene i segnaposto piu' comuni, cosi'
        // si vede sia quale file e' stato scelto sia che la sostituzione e' avvenuta.
        foreach (var nome in TemplateNomi)
            File.WriteAllText(Path.Combine(dir, nome),
                $"TEMPLATE:{nome}|[RagioneSociale]|[IdContratto]|[TipologiaFattura]|[Totale]\r\n|[ElencoMesi]");
    }

    [OneTimeTearDown]
    public void RimuoviTemplateFinti()
    {
        try { Directory.Delete(_rootFinta, recursive: true); } catch { /* best effort */ }
    }

    private DocumentBuilder BuilderFinto() => new(_rootFinta);

    /// <summary>La cartella di output dei test: i template ci arrivano dai progetti referenziati.</summary>
    private static DocumentBuilder BuilderReale() => new(TestContext.CurrentContext.TestDirectory);

    private static string Template(string? html) => html!.Split('|')[0];

    // =============================================================================================
    // REL — CreateModuloRelHtml / CreateModuloRelPdf
    // =============================================================================================

    private static RelDocumentoDto Rel(string? tipologia) => new()
    {
        TipologiaFattura = tipologia,
        Anno = "2026",
        Mese = "MAGGIO",
        RagioneSociale = "Comune di Prova",
        IdContratto = "TOKEN-E1",
        // Come RelExtensions.Map: decimal.ToString() nella cultura corrente, riletto da Replace con
        // decimal.Parse nella stessa cultura.
        Totale = 1234.56m.ToString(),
        TotaleAnalogico = 1000.5m.ToString(),
        TotaleDigitale = 234.06m.ToString(),
        TotaleNotificheAnalogiche = 70,
        TotaleNotificheDigitali = 30,
    };

    [TestCase(TipologiaFattura.PRIMOSALDO, "PRIMO_SALDO_rel.html")]
    [TestCase(TipologiaFattura.SECONDOSALDO, "SECONDO_SALDO_rel.html")]
    [TestCase(TipologiaFattura.SEMESTRALE_SOSPESI, "SEM_SOSPESI_rel.html")] // PF-997
    public void CreateModuloRelHtml_TipologiaConTemplate_UsaIlTemplateDellaTipologia(string tipologia, string atteso)
    {
        var html = BuilderFinto().CreateModuloRelHtml(Rel(tipologia));

        Assert.That(Template(html), Is.EqualTo($"TEMPLATE:{atteso}"));
        Assert.That(html, Does.Contain("|Comune di Prova|TOKEN-E1|" + tipologia + "|"));
    }

    [TestCase(TipologiaFattura.VAR_SEMESTRALE)]
    [TestCase(TipologiaFattura.VAR_ANNUALE)]
    [TestCase(TipologiaFattura.ACCONTO)]
    [TestCase(TipologiaFattura.ANTICIPO)]
    [TestCase(TipologiaFattura.ASSEVERAZIONE)]
    [TestCase("primo saldo")]       // il confronto e' esatto: il minuscolo non e' PRIMO SALDO
    [TestCase(" PRIMO SALDO")]
    [TestCase("SEM.SOSPESI")]       // senza spazio
    [TestCase("SEM-SOSPESI")]       // forma della chiave di rotta, non deserializzata
    [TestCase("")]
    [TestCase(null)]
    public void CreateModuloRel_TipologiaSenzaTemplate_LanciaTipologiaNonValida(string? tipologia)
    {
        var builder = BuilderFinto();

        Assert.Multiple(() =>
        {
            Assert.That(() => builder.CreateModuloRelHtml(Rel(tipologia)),
                Throws.Exception.With.Message.EqualTo("Tipologia fattura non valida"));
            // Il PDF fa la stessa scelta PRIMA di convertire: niente chiamata alle librerie native.
            Assert.That(() => builder.CreateModuloRelPdf(Rel(tipologia)),
                Throws.Exception.With.Message.EqualTo("Tipologia fattura non valida"));
        });
    }

    [Test]
    public void CreateModuloRel_LeTipologieConTemplate_SonoEsattamenteTre()
    {
        // Contro-prova di completezza: ogni costante di TipologiaFattura e' o fra le tre con template
        // o fra quelle che devono dare errore. Una tipologia nuova rompe il test e obbliga a decidere.
        var conTemplate = new[] { TipologiaFattura.PRIMOSALDO, TipologiaFattura.SECONDOSALDO, TipologiaFattura.SEMESTRALE_SOSPESI };
        var tutte = typeof(TipologiaFattura)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        var builder = BuilderFinto();
        var accettate = tutte.Where(t =>
        {
            try { builder.CreateModuloRelHtml(Rel(t)); return true; }
            catch (Exception ex) when (ex.Message == "Tipologia fattura non valida") { return false; }
        }).ToList();

        Assert.That(accettate, Is.EquivalentTo(conTemplate));
    }

    [TestCase(TipologiaFattura.PRIMOSALDO)]
    [TestCase(TipologiaFattura.SECONDOSALDO)]
    [TestCase(TipologiaFattura.SEMESTRALE_SOSPESI)]
    [SetCulture("it-IT")]
    public void CreateModuloRelHtml_TemplateVero_SostituisceTuttiISegnaposto(string tipologia)
    {
        var html = BuilderReale().CreateModuloRelHtml(Rel(tipologia))!;

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("Comune di Prova"));
            Assert.That(html, Does.Contain("TOKEN-E1"));
            Assert.That(html, Does.Contain("MAGGIO"));
            Assert.That(html, Does.Contain("2026"));
            Assert.That(html, Does.Contain("1.234,56"), "Totale formattato it-IT");
            Assert.That(html, Does.Contain("1.000,50"), "Totale analogico formattato it-IT");
            Assert.That(html, Does.Contain("234,06"), "Totale digitale formattato it-IT");
            AssertNessunSegnaposto(html, "[Anno]", "[Mese]", "[RagioneSociale]", "[IdContratto]", "[Totale]",
                "[TotaleAnalogico]", "[TotaleDigitale]", "[TotaleNotificheAnalogiche]", "[TotaleNotificheDigitali]");
        });
    }

    [Test]
    public void CreateModuloRelHtml_SemSospesi_TemplateVeroDiversoDaiSaldi()
    {
        // PF-997: SEM. SOSPESI ha un template proprio, non una copia di un saldo.
        var builder = BuilderReale();
        var semSospesi = builder.CreateModuloRelHtml(Rel(TipologiaFattura.SEMESTRALE_SOSPESI));
        var primo = builder.CreateModuloRelHtml(Rel(TipologiaFattura.PRIMOSALDO));
        var secondo = builder.CreateModuloRelHtml(Rel(TipologiaFattura.SECONDOSALDO));

        Assert.That(semSospesi, Is.Not.EqualTo(primo).And.Not.EqualTo(secondo));
    }

    [TestCase("it-IT")]
    [TestCase("en-US")]
    [TestCase("")] // InvariantCulture, come un container Linux senza cultura impostata
    public void CreateModuloRelHtml_ImportiFormattatiItaliano_InQualunqueCulturaDelProcesso(string cultura)
    {
        // Map scrive gli importi con decimal.ToString() e Replace li rilegge con decimal.Parse, entrambi
        // nella cultura corrente: il giro e' coerente in qualunque cultura, e l'output e' sempre it-IT.
        var precedente = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultura);
            var html = BuilderReale().CreateModuloRelHtml(Rel(TipologiaFattura.SEMESTRALE_SOSPESI))!;
            Assert.That(html, Does.Contain("1.234,56"));
        }
        finally
        {
            CultureInfo.CurrentCulture = precedente;
        }
    }

    [Test]
    public void CreateModuloRelHtml_TotaleNullo_ArgumentNull_Caratterizzazione()
    {
        // Replace fa decimal.Parse(model.Totale!) senza guardia: un totale assente non produce un
        // documento con "0,00" ma un'eccezione (500 sulla rotta). Oggi RelTestata.Totale e' NOT NULL,
        // quindi non succede; il test documenta che la protezione sta nel DB, non nel builder.
        var rel = Rel(TipologiaFattura.PRIMOSALDO);
        rel.Totale = null;

        Assert.That(() => BuilderFinto().CreateModuloRelHtml(rel), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void CreateModuloRelHtml_RagioneSocialeConMarkup_InseritaSenzaEscape_Caratterizzazione()
    {
        // I valori vengono inseriti nell'HTML cosi' come sono, senza HtmlEncode. La ragione sociale
        // arriva da SelfCare (pfd.Enti), non dall'utente del portale: rischio basso, ma una "&" o un
        // "<" finiscono nel markup del documento. Se si decide di fare l'escape, aggiornare il test.
        var rel = Rel(TipologiaFattura.SEMESTRALE_SOSPESI);
        rel.RagioneSociale = "Comune di A & B <script>x</script>";

        var html = BuilderReale().CreateModuloRelHtml(rel)!;

        Assert.That(html, Does.Contain("Comune di A & B <script>x</script>"));
    }

    [TestCase(TipologiaFattura.PRIMOSALDO)]
    [TestCase(TipologiaFattura.SEMESTRALE_SOSPESI)]
    public void CreateModuloRel_TemplateMancante_ConfigurationException(string tipologia)
    {
        var vuota = Path.Combine(Path.GetTempPath(), "pfw-tests", "vuota-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(vuota, Documenti));
        try
        {
            var builder = new DocumentBuilder(vuota);
            Assert.That(() => builder.CreateModuloRelHtml(Rel(tipologia)), Throws.TypeOf<ConfigurationException>());
        }
        finally
        {
            Directory.Delete(vuota, recursive: true);
        }
    }

    // =============================================================================================
    // PEC — CreateEmailHtml
    // =============================================================================================

    private static RelEmail Email(string? tipoContratto, bool? fatturata, int? righe, string? tipologia = "PRIMO SALDO") => new()
    {
        TipoContratto = tipoContratto,
        FlagFatturata = fatturata,
        NumeroRighe = righe,
        TipologiaFattura = tipologia,
        Anno = 2026,
        Mese = 5,
        IdEnte = "ente",
        IdContratto = "TOKEN-E1",
        Pec = "ente@pec.it",
        RagioneSociale = "Comune di Prova",
        Semestre = "primo semestre",
        ElencoMesi = "03/2026, 04/2026",
    };

    [TestCase("PAC", false, null, "pac_pec_sospesa.html")]
    [TestCase("PAC", false, 3, "pac_pec_sospesa.html")]
    [TestCase("PAC", true, 1, "pac_pec_emessa_singola.html")]
    [TestCase("PAC", true, 2, "pac_pec_emesse_multiple.html")]
    [TestCase("pac", true, 5, "pac_pec_emesse_multiple.html")] // confronto case-insensitive
    [TestCase("PAL", false, null, "pal_pec_sospesa.html")]
    [TestCase("PAL", true, 1, "pal_pec_emessa_singola.html")]
    [TestCase("PAL", true, 7, "pal_pec_emesse_multiple.html")]
    [TestCase("Pal", true, 1, "pal_pec_emessa_singola.html")]
    public void CreateEmailHtml_ContrattoPacPal_ScegliePecPerStatoENumeroRighe(string contratto, bool fatturata, int? righe, string atteso)
    {
        var html = BuilderFinto().CreateEmailHtml(Email(contratto, fatturata, righe));

        Assert.That(Template(html), Is.EqualTo($"TEMPLATE:{atteso}"));
    }

    [TestCase(null, null, null, "PRIMO SALDO", "primo_saldo.html")]
    [TestCase("", true, 1, "SECONDO SALDO", "secondo_saldo.html")]
    [TestCase("ALTRO", true, 1, "VAR. SEMESTRALE", "var_semestrale.html")]
    // PAC/PAL ma combinazione non coperta dai rami: si ricade sul template per tipologia.
    [TestCase("PAC", null, 1, "PRIMO SALDO", "primo_saldo.html")]
    [TestCase("PAC", true, 0, "SECONDO SALDO", "secondo_saldo.html")]
    [TestCase("PAL", true, null, "VAR. SEMESTRALE", "var_semestrale.html")]
    // Ricerca per sottostringa, case-insensitive: qualunque tipologia senza "primo"/"secondo"
    // finisce su var_semestrale, anche se non e' una variazione.
    [TestCase(null, null, null, "SEM. SOSPESI", "var_semestrale.html")]
    [TestCase(null, null, null, "ACCONTO", "var_semestrale.html")]
    [TestCase(null, null, null, "primo saldo", "primo_saldo.html")]
    public void CreateEmailHtml_SenzaRamoPacPal_RicadeSulTemplatePerTipologia(string? contratto, bool? fatturata, int? righe, string tipologia, string atteso)
    {
        var html = BuilderFinto().CreateEmailHtml(Email(contratto, fatturata, righe, tipologia));

        Assert.That(Template(html), Is.EqualTo($"TEMPLATE:{atteso}"));
    }

    [Test]
    public void CreateEmailHtml_RimuoveIRitorniACapo()
    {
        var html = BuilderFinto().CreateEmailHtml(Email("PAC", false, null))!;

        Assert.That(html, Does.Not.Contain("\r").And.Not.Contain("\n"));
        Assert.That(html, Does.Contain("|03/2026, 04/2026"), "ElencoMesi sostituito");
    }

    [Test]
    public void CreateEmailHtml_TipologiaNullaSenzaRamoPacPal_NullReference_Caratterizzazione()
    {
        // Il ripiego per tipologia fa dati.TipologiaFattura!.ToLower() senza guardia. Oggi la
        // tipologia arriva sempre dalla vista pfd.EmailRel; il test documenta che un null non
        // produce un errore leggibile.
        Assert.That(() => BuilderFinto().CreateEmailHtml(Email(null, null, null, tipologia: null)),
            Throws.TypeOf<NullReferenceException>());
    }

    [TestCase("PAC", false, null)]
    [TestCase("PAC", true, 1)]
    [TestCase("PAC", true, 2)]
    [TestCase("PAL", false, null)]
    [TestCase("PAL", true, 1)]
    [TestCase("PAL", true, 2)]
    [TestCase(null, null, null)]
    public void CreateEmailHtml_TemplateVero_SostituisceISegnaposto(string? contratto, bool? fatturata, int? righe)
    {
        foreach (var tipologia in new[] { "PRIMO SALDO", "SECONDO SALDO", "VAR. SEMESTRALE" })
        {
            var html = BuilderReale().CreateEmailHtml(Email(contratto, fatturata, righe, tipologia))!;

            Assert.Multiple(() =>
            {
                Assert.That(html, Does.Contain("Comune di Prova"), tipologia);
                AssertNessunSegnaposto(html, "[RagioneSociale]", "[ElencoMesi]", "[Anno]", "[Mese]", "[Semestre]");
            });
        }
    }

    // =============================================================================================
    // Dettaglio documento contabile SOSPESO
    // =============================================================================================

    private static DocumentoContabileSospeso Sospeso(string? contratto, string? tipologia = "ACCONTO") => new()
    {
        TipologiaContratto = contratto,
        TipologiaFattura = tipologia,
        RagioneSociale = "Comune di Prova",
        IdContratto = "TOKEN-E1",
        Anno = 2026,
        Mese = 5,
        Totale = 1234.56m,
        TotaleAnalogico = 1000m,
        TotaleDigitale = 234.56m,
        StornoAnalogico = -10m,
        StornoDigitale = -5m,
        TotaleStorno = -15m,
        ImportoSottoSoglia = 8.11m,
        TotaleNotificheAnalogiche = 70,
        TotaleNotificheDigitali = 30,
    };

    [TestCase("PAC", "pac_fattura_sospesa_singola V2.html")]
    [TestCase("pac", "pac_fattura_sospesa_singola V2.html")]
    [TestCase("PAL", "pal_fattura_sospesa_singola V2.html")]
    [TestCase("CONTRATTO PAL", "pal_fattura_sospesa_singola V2.html")] // Contains, non uguaglianza
    [TestCase("PAC/PAL", "pac_fattura_sospesa_singola V2.html")]       // PAC ha la precedenza
    public void CreateDettaglioFatturaSospesaHtml_ScegliePerContratto(string contratto, string atteso)
    {
        var html = BuilderFinto().CreateDettaglioFatturaSospesaHtml(Sospeso(contratto));

        Assert.That(Template(html), Is.EqualTo($"TEMPLATE:{atteso}"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("ALTRO")]
    public void CreateDettaglioFatturaSospesa_ContrattoNonValido_Lancia(string? contratto)
    {
        var builder = BuilderFinto();

        Assert.Multiple(() =>
        {
            Assert.That(() => builder.CreateDettaglioFatturaSospesaHtml(Sospeso(contratto)),
                Throws.Exception.With.Message.EqualTo($"Tipologia Contratto non valida {contratto ?? string.Empty}"));
            Assert.That(() => builder.CreateDettaglioFatturaSospesaPdf(Sospeso(contratto)),
                Throws.Exception.With.Message.StartsWith("Tipologia Contratto non valida"));
        });
    }

    [TestCase("PRIMO SALDO", "")]
    [TestCase("SECONDO SALDO", " SECONDO SALDO Contestazioni risolte")]
    [TestCase("ACCONTO", " ACCONTO")]
    [TestCase("SEM. SOSPESI", " SEM. SOSPESI")]
    [TestCase(null, "")]
    [TestCase("   ", "")]
    public void CreateDettaglioFatturaSospesaHtml_TipologiaNelTitolo(string? tipologia, string attesa)
    {
        var html = BuilderFinto().CreateDettaglioFatturaSospesaHtml(Sospeso("PAC", tipologia))!;

        Assert.That(html.Split('|')[3], Is.EqualTo(attesa));
    }

    [Test]
    public void CreateDettaglioFatturaSospesaHtml_ModificaLaTipologiaDelChiamante_Caratterizzazione()
    {
        // BuildTipologiaFattura riscrive dati.TipologiaFattura sull'oggetto ricevuto: richiamare il
        // builder sullo stesso oggetto aggiunge il suffisso due volte. Oggi ogni rotta costruisce un
        // oggetto nuovo per chiamata, quindi non si vede; se si riusa il DTO (es. HTML + PDF), si'.
        var dati = Sospeso("PAC", "SECONDO SALDO");
        var builder = BuilderFinto();

        builder.CreateDettaglioFatturaSospesaHtml(dati);
        Assert.That(dati.TipologiaFattura, Is.EqualTo(" SECONDO SALDO Contestazioni risolte"));

        var seconda = builder.CreateDettaglioFatturaSospesaHtml(dati)!;
        Assert.That(seconda.Split('|')[3], Is.EqualTo("  SECONDO SALDO Contestazioni risolte"));
    }

    [TestCase("PAC")]
    [TestCase("PAL")]
    public void CreateDettaglioFatturaSospesaHtml_TemplateVero_SostituisceISegnaposto(string contratto)
    {
        var html = BuilderReale().CreateDettaglioFatturaSospesaHtml(Sospeso(contratto))!;

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("Comune di Prova"));
            Assert.That(html, Does.Contain("05/2026"), "ElencoMesi");
            Assert.That(html, Does.Contain("1.234,56"));
            Assert.That(html, Does.Contain("8,11"), "importo sotto soglia");
            AssertNessunSegnaposto(html, "[RagioneSociale]", "[IdContratto]", "[ElencoMesi]", "[Totale]",
                "[TotaleAnalogico]", "[TotaleDigitale]", "[TotaleStorno]", "[StornoAnalogico]", "[StornoDigitale]",
                "[ImportoSottoSoglia]", "[TipologiaFattura]", "[TotaleNotificheAnalogiche]", "[TotaleNotificheDigitali]");
        });
    }

    [Test]
    public void CreateDettaglioFatturaSospesaHtml_StorniInValoreAssoluto()
    {
        var html = BuilderReale().CreateDettaglioFatturaSospesaHtml(Sospeso("PAC"))!;

        Assert.That(html, Does.Contain("15,00").And.Not.Contain("-15,00"));
    }

    // =============================================================================================
    // Dettaglio documento contabile EMESSO (singolo)
    // =============================================================================================

    private static DocumentoContabileEmesso Emesso(string? contratto, string? tipologia = "PRIMO SALDO") => new()
    {
        TipologiaContratto = contratto,
        TipologiaFattura = tipologia,
        RagioneSociale = "Comune di Prova",
        IdContratto = "TOKEN-E1",
        Anno = "2026",
        Mese = "5",
        Totale = 1234.56m,
        TotaleAnalogico = 1000m,
        TotaleDigitale = 234.56m,
        TotaleAnticipo = 300m,
        AnticipoAnalogico = 200m,
        AnticipoDigitale = 100m,
        TotaleAcconto = 50m,
        AccontoAnalogico = 30m,
        AccontoDigitale = 20m,
        Imponibile = 884.56m,
        TotaleNotificheAnalogiche = 70,
        TotaleNotificheDigitali = 30,
    };

    [TestCase("PAC", "pac_fattura_emessa_singola V2.html")]
    [TestCase("PAL", "pal_fattura_emessa_singola V2.html")]
    [TestCase("pal", "pal_fattura_emessa_singola V2.html")]
    public void CreateDettaglioFatturaEmessaHtml_ScegliePerContratto(string contratto, string atteso)
    {
        var html = BuilderFinto().CreateDettaglioFatturaEmessaHtml(Emesso(contratto));

        Assert.That(Template(html), Is.EqualTo($"TEMPLATE:{atteso}"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("ALTRO")]
    public void CreateDettaglioFatturaEmessa_ContrattoNonValido_Lancia(string? contratto)
    {
        var builder = BuilderFinto();

        Assert.Multiple(() =>
        {
            Assert.That(() => builder.CreateDettaglioFatturaEmessaHtml(Emesso(contratto)),
                Throws.Exception.With.Message.StartsWith("Tipologia Contratto non valida"));
            Assert.That(() => builder.CreateDettaglioFatturaEmessaPdf(Emesso(contratto)),
                Throws.Exception.With.Message.StartsWith("Tipologia Contratto non valida"));
        });
    }

    [TestCase("PRIMO SALDO", "")]
    [TestCase("SECONDO SALDO", " SECONDO SALDO Contestazioni risolte")]
    [TestCase("ANTICIPO", " ANTICIPO")]
    public void CreateDettaglioFatturaEmessaHtml_TipologiaNelTitolo(string tipologia, string attesa)
    {
        var html = BuilderFinto().CreateDettaglioFatturaEmessaHtml(Emesso("PAL", tipologia))!;

        Assert.That(html.Split('|')[3], Is.EqualTo(attesa));
    }

    [TestCase("PAC")]
    [TestCase("PAL")]
    [SetCulture("it-IT")] // l'Api imposta it-IT come cultura di default (CultureConfiguration.UseCulture)
    public void CreateDettaglioFatturaEmessaHtml_TemplateVero_SostituisceISegnaposto(string contratto)
    {
        var html = BuilderReale().CreateDettaglioFatturaEmessaHtml(Emesso(contratto))!;

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("Comune di Prova"));
            Assert.That(html, Does.Contain("05/2026"));
            Assert.That(html, Does.Contain("1.234,56"));
            Assert.That(html, Does.Contain("884,56"), "imponibile");
            AssertNessunSegnaposto(html, "[RagioneSociale]", "[IdContratto]", "[ElencoMesi]", "[Totale]",
                "[TotaleAnalogico]", "[TotaleDigitale]", "[TotaleAnticipo]", "[AnticipoAnalogico]", "[AnticipoDigitale]",
                "[TotaleAcconto]", "[AccontoAnalogico]", "[AccontoDigitale]", "[Imponibile]", "[TipologiaFattura]",
                "[TotaleNotificheAnalogiche]", "[TotaleNotificheDigitali]");
        });
    }

    [Test]
    [SetCulture("en-US")]
    public void CreateDettaglioFatturaEmessaHtml_ImportiParzialiSeguonoLaCulturaDelProcesso_Caratterizzazione()
    {
        // Nel Replace dell'emesso solo [Totale] e' formattato esplicitamente it-IT; gli altri importi
        // usano ToString("N2") nella cultura del thread. Nell'Api la cultura e' it-IT e il documento
        // e' coerente; in un processo con un'altra cultura i due formati si mescolano.
        var html = BuilderReale().CreateDettaglioFatturaEmessaHtml(Emesso("PAC"))!;

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("1.234,56"), "Totale: sempre it-IT");
            Assert.That(html, Does.Contain("1,000.00"), "TotaleAnalogico: cultura del processo");
        });
    }

    // =============================================================================================
    // Dettaglio documenti contabili EMESSI MULTIPLI
    // =============================================================================================

    private static DocumentoContabileEmessiMultipli Multipli(string? contratto) => new()
    {
        TipologiaContratto = contratto,
        TipologiaFattura = "PRIMO SALDO",
        RagioneSociale = "Comune di Prova",
        IdContratto = "TOKEN-E1",
        Imponibile = 2000m,
        DettaglioFatture =
        [
            new() { Anno = "2026", Mese = "3", Totale = 1000m, TotaleDigitale = 400m, TotaleAnalogico = 600m,
                    TotaleNotificheDigitali = 4, TotaleNotificheAnalogiche = 6, TipologiaContratto = contratto,
                    TipologiaFattura = "SECONDO SALDO", TotaleFatturaSospesaImponibile = 900m },
            new() { Anno = "2026", Mese = "4", Totale = 1000m, TotaleDigitale = 500m, TotaleAnalogico = 500m,
                    TotaleNotificheDigitali = 5, TotaleNotificheAnalogiche = 5, TipologiaContratto = contratto,
                    TipologiaFattura = "PRIMO SALDO", TotaleFatturaSospesaImponibile = 1100m },
        ],
    };

    [TestCase("PAC", "pac_fatture_emesse_multiple V2.html")]
    [TestCase("PAL", "pal_fatture_emesse_multiple V2.html")]
    public void CreateDettaglioFatturaEmessaMultiplaHtml_ScegliePerContratto(string contratto, string atteso)
    {
        var html = BuilderFinto().CreateDettaglioFatturaEmessaMultiplaHtml(Multipli(contratto));

        Assert.That(Template(html), Is.EqualTo($"TEMPLATE:{atteso}"));
        Assert.That(html, Does.Contain("|03/2026, 04/2026"), "ElencoMesi dai mesi del dettaglio");
    }

    [TestCase(null)]
    [TestCase("ALTRO")]
    public void CreateDettaglioFatturaEmessaMultipla_ContrattoNonValido_Lancia(string? contratto)
    {
        var builder = BuilderFinto();

        Assert.Multiple(() =>
        {
            Assert.That(() => builder.CreateDettaglioFatturaEmessaMultiplaHtml(Multipli(contratto)),
                Throws.Exception.With.Message.StartsWith("Tipologia Contratto non valida"));
            Assert.That(() => builder.CreateDettaglioFatturaEmessaMultiplaPdf(Multipli(contratto)),
                Throws.Exception.With.Message.StartsWith("Tipologia Contratto non valida"));
        });
    }

    [Test]
    public void CreateDettaglioFatturaEmessaMultiplaHtml_NonRiscriveLaTipologia()
    {
        // A differenza di singola e sospesa, il multiplo non passa da BuildTipologiaFattura: la
        // tipologia per mese la scrive GetDettaglioMesi.
        var dati = Multipli("PAC");
        BuilderFinto().CreateDettaglioFatturaEmessaMultiplaHtml(dati);

        Assert.That(dati.TipologiaFattura, Is.EqualTo("PRIMO SALDO"));
    }

    [TestCase("PAC", true)]
    [TestCase("PAL", false)]
    public void CreateDettaglioFatturaEmessaMultiplaHtml_TemplateVero_DettaglioPerMese(string contratto, bool conAcconto)
    {
        var html = BuilderReale().CreateDettaglioFatturaEmessaMultiplaHtml(Multipli(contratto))!;

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("MESE DI 03/2026 SECONDO SALDO Contestazioni risolte"));
            Assert.That(html, Does.Contain("MESE DI 04/2026<"), "PRIMO SALDO: nessun suffisso");
            Assert.That(html, Does.Contain("03/2026, 04/2026"));
            Assert.That(html.Contains("acconto delle notifiche"), Is.EqualTo(conAcconto),
                "La riga dell'acconto c'e' solo per i PAC");
            AssertNessunSegnaposto(html, "[RagioneSociale]", "[IdContratto]", "[ElencoMesi]", "[Imponibile]",
                "[DettaglioMesiV2]", "[DettaglioMesi]");
        });
    }

    // =============================================================================================
    // Modulo commessa
    // =============================================================================================

    private static ModuloCommessaDocumentoDto Commessa() => new()
    {
        Descrizione = "Comune di Prova",
        PartitaIva = "01234567890",
        IndirizzoCompleto = "Via Roma 1, Roma",
        Cup = "CUP-1",
        CodCommessa = "COMM-1",
        SplitPayment = "Si",
        Map = "MAP-1",
        TipoCommessa = "Ordine",
        Prodotto = "prod-pn",
        Pec = "ente@pec.it",
        MeseAttivita = "MAGGIO",
        DataModifica = new DateTimeOffset(2026, 5, 10, 14, 30, 0, TimeSpan.FromHours(2)),
        Contatti = [new DatiFatturazioneContatto { Email = "uno@ente.it" }, new DatiFatturazioneContatto { Email = "due@ente.it" }],
        DatiModuloCommessa =
        [
            new() { IdTipoSpedizione = 1, Tipo = "Analogico AR", NumeroNotificheNazionali = 10, NumeroNotificheInternazionali = 1, TotaleNotifiche = 11 },
            new() { IdTipoSpedizione = 3, Tipo = "Digitale", NumeroNotificheNazionali = 100, NumeroNotificheInternazionali = 0, TotaleNotifiche = 100 },
        ],
        DatiModuloCommessaCosti = [new() { Descrizione = "Totale digitale", Totale = 100m }],
    };

    [Test]
    public void CreateModuloCommessaHtml_UsaDaticommessa()
    {
        var html = BuilderFinto().CreateModuloCommessaHtml(Commessa());

        Assert.That(Template(html), Is.EqualTo("TEMPLATE:daticommessa.html"));
    }

    [Test]
    public void CreateModuloCommessaHtml_TemplateVero_SostituisceISegnaposto()
    {
        var html = BuilderReale().CreateModuloCommessaHtml(Commessa())!;

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("Comune di Prova"));
            Assert.That(html, Does.Contain("01234567890"));
            Assert.That(html, Does.Contain("mailto:ente@pec.it"));
            Assert.That(html, Does.Contain("uno@ente.it").And.Contain("due@ente.it"));
            Assert.That(html, Does.Contain("Digitale").And.Contain("Analogico AR"));
            Assert.That(html, Does.Contain("10/05/2026"), "DataModifica in formato it-IT");
            AssertNessunSegnaposto(html, "[Descrizione]", "[PartitaIva]", "[IndirizzoCompleto]", "[Cup]",
                "[SplitPayment]", "[Prodotto]", "[Pec]", "[Contatti]", "[DataModifica]", "[MeseAttivita]",
                "[DatiModuloCommessa]", "[DatiModuloCommessaCosti]");
        });
    }

    [Test]
    public void CreateModuloCommessaHtml_TipiSpedizioneInOrdineDigitalePoiAnalogico()
    {
        // GetModuloCommessa ordina per IdTipoSpedizione 3, 1, 2, 0 indipendentemente dall'input.
        var html = BuilderReale().CreateModuloCommessaHtml(Commessa())!;

        Assert.That(html.IndexOf("Digitale", StringComparison.Ordinal),
            Is.LessThan(html.IndexOf("Analogico AR", StringComparison.Ordinal)));
    }

    [Test]
    public void CreateModuloCommessaHtml_SenzaCosti_ArgumentNull_Caratterizzazione()
    {
        // DatiModuloCommessaCosti! senza guardia: un modulo senza totali di costo non genera il
        // documento (Count() su null in GetModuloCommessaTotali). Il Mapper li valorizza sempre; il
        // test documenta che la guardia non c'e'.
        var dati = Commessa();
        dati.DatiModuloCommessaCosti = null;

        Assert.That(() => BuilderFinto().CreateModuloCommessaHtml(dati), Throws.TypeOf<ArgumentNullException>());
    }

    // =============================================================================================
    // Template presenti nell'output
    // =============================================================================================

    [Test]
    public void TuttiITemplateDelBuilder_SonoNellOutputDellaBuild()
    {
        // I nomi letti per reflection dai campi del builder, non dall'elenco di questa classe: un
        // template nuovo entra nel controllo da solo. Complementare a DockerfileGuardrailsTests, che
        // guarda i csproj; questo guarda il risultato della build.
        var citati = typeof(DocumentBuilder)
            .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(v => v is not null && v.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            .Select(v => v!)
            .ToList();

        var cartella = Path.Combine(TestContext.CurrentContext.TestDirectory, Documenti);
        var mancanti = citati.Where(n => !File.Exists(Path.Combine(cartella, n))).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(citati, Is.EquivalentTo(TemplateNomi), "Elenco dei template del builder cambiato: aggiornare TemplateNomi.");
            Assert.That(mancanti, Is.Empty, $"Template non copiati nell'output: {string.Join(", ", mancanti)}");
        });
    }

    // =============================================================================================
    // PDF — smoke
    // =============================================================================================

    [TestCase(TipologiaFattura.PRIMOSALDO)]
    [TestCase(TipologiaFattura.SEMESTRALE_SOSPESI)]
    public void CreateModuloRelPdf_TemplateVero_ProducePdf(string tipologia)
    {
        var pdf = PdfOppureInconcludente(() => BuilderReale().CreateModuloRelPdf(Rel(tipologia)));

        AssertPdf(pdf);
    }

    [Test]
    public void CreateDettagliPdf_TemplateVeri_ProduconoPdf()
    {
        var builder = BuilderReale();

        Assert.Multiple(() =>
        {
            AssertPdf(PdfOppureInconcludente(() => builder.CreateDettaglioFatturaSospesaPdf(Sospeso("PAL"))));
            AssertPdf(PdfOppureInconcludente(() => builder.CreateDettaglioFatturaEmessaPdf(Emesso("PAC"))));
            AssertPdf(PdfOppureInconcludente(() => builder.CreateDettaglioFatturaEmessaMultiplaPdf(Multipli("PAL"))));
            AssertPdf(PdfOppureInconcludente(() => builder.CreateModuloCommessaPdf(Commessa())));
        });
    }

    // --- helper --------------------------------------------------------------------------------

    /// <summary>
    /// Le librerie native di wkhtmltopdf possono mancare sulla macchina che esegue i test (non sul
    /// runtime, dove le garantisce DockerfileGuardrailsTests): in quel caso il test e' inconcludente,
    /// non rosso.
    /// </summary>
    private static byte[] PdfOppureInconcludente(Func<byte[]> genera)
    {
        try
        {
            return genera();
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException
                                   || ex.InnerException is DllNotFoundException or BadImageFormatException)
        {
            Assert.Inconclusive($"Librerie native PDF non disponibili su questa macchina: {ex.Message}");
            throw;
        }
    }

    private static void AssertPdf(byte[] pdf)
    {
        Assert.That(pdf, Is.Not.Null.And.Length.GreaterThan(1000));
        Assert.That(System.Text.Encoding.ASCII.GetString(pdf, 0, 5), Is.EqualTo("%PDF-"));
    }

    private static void AssertNessunSegnaposto(string html, params string[] segnaposto)
    {
        var rimasti = segnaposto.Where(s => html.Contains(s, StringComparison.Ordinal)).ToList();
        Assert.That(rimasti, Is.Empty, $"Segnaposto non sostituiti: {string.Join(", ", rimasti)}");
    }
}
