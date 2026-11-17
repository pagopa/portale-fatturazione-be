using System.Text;
using MimeKit;
using PortaleFatture.BE.Infrastructure.Gateway.Email;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Composizione del messaggio per l'API Gmail (email ai PSP e alert): la parte di
/// <see cref="PspEmailSender"/> e <see cref="AlertSender"/> che non chiama Google. Il messaggio viene
/// sempre riletto dalla sua forma "raw", cioe' da cio' che verrebbe spedito, non dall'oggetto in memoria.
///
/// Sono anche i test che esercitano MimeKit (e quindi il ramo MailKit del grafo) dopo un aggiornamento
/// dei pacchetti: se la libreria cambiasse il modo di scrivere intestazioni o corpo, falliscono qui.
/// </summary>
[Category("Email")]
public class GmailMessageBuilderTests
{
    private const string Mittente = "noreply@portalefatture.invalid";
    private const string Destinatario = "psp.test@portalefatture.invalid";

    private static MimeMessage Componi(
        string? oggetto = "Oggetto", string? corpo = "<p>corpo</p>",
        string? nomeDestinatario = "PSP di prova", string? destinatario = Destinatario,
        string? nomeMittente = "Portale Fatturazione", string? mittente = Mittente)
        => GmailMessageBuilder.Componi(nomeMittente, mittente, nomeDestinatario, destinatario, oggetto, corpo);

    /// <summary>Decodifica il "raw" come farebbe Gmail: base64url senza padding, poi parsing MIME.</summary>
    private static MimeMessage Rileggi(string raw)
    {
        var base64 = raw.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        using var stream = new MemoryStream(Convert.FromBase64String(base64));
        return MimeMessage.Load(stream);
    }

    /// <summary>
    /// Corpo HTML riletto. MimeKit chiude il corpo con un a-capo quando lo scrive: e' la forma
    /// canonica di una parte testuale, non contenuto aggiunto, e qui viene tolto per il confronto.
    /// </summary>
    private static string Corpo(MimeMessage riletto)
    {
        var html = riletto.HtmlBody ?? string.Empty;
        return html.EndsWith("\r\n", StringComparison.Ordinal) ? html[..^2] : html;
    }

    private static MimeMessage GiroCompleto(MimeMessage messaggio) => Rileggi(GmailMessageBuilder.ToRaw(messaggio));

    // ------------------------------------------------------------------ composizione

    [Test]
    public void Componi_ShouldPortareMittenteDestinatarioOggettoECorpoHtml()
    {
        var riletto = GiroCompleto(Componi("Report trimestrale", "<h1>Titolo</h1><p>Testo</p>"));

        Assert.Multiple(() =>
        {
            var da = riletto.From.Mailboxes.Single();
            var a = riletto.To.Mailboxes.Single();
            Assert.That(da.Address, Is.EqualTo(Mittente));
            Assert.That(da.Name, Is.EqualTo("Portale Fatturazione"));
            Assert.That(a.Address, Is.EqualTo(Destinatario));
            Assert.That(a.Name, Is.EqualTo("PSP di prova"));
            Assert.That(riletto.Subject, Is.EqualTo("Report trimestrale"));
            Assert.That(Corpo(riletto), Is.EqualTo("<h1>Titolo</h1><p>Testo</p>"));
            Assert.That(riletto.TextBody, Is.Null, "il corpo e' solo HTML, senza alternativa testuale");
            Assert.That(riletto.Cc, Is.Empty);
            Assert.That(riletto.Bcc, Is.Empty);
            Assert.That(riletto.Attachments, Is.Empty);
        });
    }

    [Test]
    public void Componi_ShouldDichiarareHtmlInUtf8()
    {
        var corpo = (TextPart)GiroCompleto(Componi()).Body;

        Assert.Multiple(() =>
        {
            Assert.That(corpo.ContentType.MimeType, Is.EqualTo("text/html"));
            Assert.That(corpo.ContentType.Charset, Is.EqualTo("utf-8").IgnoreCase);
        });
    }

    /// <summary>Le email ai PSP sono in italiano: accenti, euro e virgolette tipografiche non si perdono.</summary>
    [TestCase("Perché è così: già fatto, più tardi")]
    [TestCase("Importo dovuto: 1.234,56 € — «saldo»")]
    [TestCase("日本語 и кириллица 🙂")]
    public void Componi_CaratteriNonAscii_ShouldSopravvivereInOggettoCorpoENome(string testo)
    {
        var riletto = GiroCompleto(Componi(oggetto: testo, corpo: $"<p>{testo}</p>", nomeDestinatario: testo));

        Assert.Multiple(() =>
        {
            Assert.That(riletto.Subject, Is.EqualTo(testo));
            Assert.That(Corpo(riletto), Is.EqualTo($"<p>{testo}</p>"));
            Assert.That(riletto.To.Mailboxes.Single().Name, Is.EqualTo(testo));
        });
    }

    [Test]
    public void Componi_NomiAssenti_ShouldLasciareIlSoloIndirizzo()
    {
        var riletto = GiroCompleto(Componi(nomeDestinatario: null, nomeMittente: null));

        Assert.Multiple(() =>
        {
            Assert.That(riletto.From.Mailboxes.Single().Address, Is.EqualTo(Mittente));
            Assert.That(riletto.To.Mailboxes.Single().Address, Is.EqualTo(Destinatario));
            Assert.That(riletto.To.Mailboxes.Single().Name, Is.Null.Or.Empty);
        });
    }

    /// <summary>
    /// Oggetto assente: la composizione lancia, e i sender lo trasformano in (messaggio, false). Un
    /// oggetto vuoto invece e' ammesso.
    /// </summary>
    [Test]
    public void Componi_OggettoAssente_ShouldLanciare_MentreVuotoEAmmesso()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Componi(oggetto: null), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(GiroCompleto(Componi(oggetto: string.Empty)).Subject, Is.Null.Or.Empty);
        });
    }

    [Test]
    public void Componi_CorpoVuoto_ShouldDareUnMessaggioValido()
    {
        var riletto = GiroCompleto(Componi(corpo: string.Empty));

        Assert.Multiple(() =>
        {
            Assert.That(Corpo(riletto), Is.Empty);
            Assert.That(riletto.To.Mailboxes.Single().Address, Is.EqualTo(Destinatario));
        });
    }

    // ------------------------------------------------------------------ codifica raw

    /// <summary>
    /// Gmail vuole base64url senza padding. Il corpo e' scelto perche' il base64 standard dei suoi
    /// byte contenga sia '+' sia '/': senza la sostituzione il test non proverebbe nulla.
    /// </summary>
    [Test]
    public void ToRaw_ShouldEssereBase64UrlSenzaPadding()
    {
        var messaggio = Componi(corpo: "<p>ÿþÿþÿþ ???>>> ~~~ ûûû ïïï</p>");
        using var grezzo = new MemoryStream();
        messaggio.WriteTo(grezzo);
        var standard = Convert.ToBase64String(grezzo.ToArray());

        var raw = GmailMessageBuilder.ToRaw(messaggio);

        Assert.Multiple(() =>
        {
            Assert.That(standard, Does.Contain("+").Or.Contain("/"), "premessa del test: servono caratteri da sostituire");
            Assert.That(raw, Does.Match("^[A-Za-z0-9_-]+$"), "solo l'alfabeto base64url, niente '+', '/', '='");
            Assert.That(raw, Is.EqualTo(standard.TrimEnd('=').Replace('+', '-').Replace('/', '_')));
        });
    }

    /// <summary>Le tre lunghezze possibili del resto (0, 1, 2 byte): il padding tolto si deve poter rimettere.</summary>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void ToRaw_QualunqueLunghezza_ShouldRileggersiIdentico(int caratteriInPiu)
    {
        var corpo = "<p>" + new string('x', caratteriInPiu) + "</p>";

        Assert.That(Corpo(GiroCompleto(Componi(corpo: corpo))), Is.EqualTo(corpo));
    }

    [Test]
    public void ToRaw_StessoMessaggio_ShouldEssereStabile()
    {
        var messaggio = Componi();

        Assert.That(GmailMessageBuilder.ToRaw(messaggio), Is.EqualTo(GmailMessageBuilder.ToRaw(messaggio)));
    }

    // ------------------------------------------------------------------ adversarial

    /// <summary>
    /// Header injection: un a-capo nell'oggetto o nel nome non deve aprire una nuova intestazione.
    /// Il messaggio riletto ha ancora un solo destinatario e nessun Bcc/Cc.
    /// </summary>
    [TestCase("Oggetto\r\nBcc: intruso@portalefatture.invalid")]
    [TestCase("Oggetto\nBcc: intruso@portalefatture.invalid")]
    [TestCase("Oggetto\r\n\r\n<p>corpo iniettato</p>")]
    [TestCase("Oggetto\rTo: intruso@portalefatture.invalid")]
    public void Componi_ACapoNellOggetto_ShouldNonAggiungereIntestazioni(string oggetto)
    {
        var riletto = GiroCompleto(Componi(oggetto: oggetto, corpo: "<p>corpo vero</p>"));

        Assert.Multiple(() =>
        {
            Assert.That(riletto.Bcc, Is.Empty, "nessun Bcc iniettato");
            Assert.That(riletto.Cc, Is.Empty);
            Assert.That(riletto.To.Mailboxes.Select(m => m.Address), Is.EqualTo(new[] { Destinatario }));
            Assert.That(riletto.Headers.Count(h => h.Id == HeaderId.Bcc), Is.Zero);
            Assert.That(Corpo(riletto), Is.EqualTo("<p>corpo vero</p>"), "il corpo non e' sostituito");
        });
    }

    [TestCase("Nome\r\nBcc: intruso@portalefatture.invalid")]
    [TestCase("Nome\" <intruso@portalefatture.invalid>, \"Altro")]
    [TestCase("Nome, intruso@portalefatture.invalid")]
    public void Componi_NomeDestinatarioOstile_ShouldRestareUnSoloDestinatario(string nome)
    {
        var riletto = GiroCompleto(Componi(nomeDestinatario: nome));

        Assert.Multiple(() =>
        {
            Assert.That(riletto.To.Mailboxes.Select(m => m.Address), Is.EqualTo(new[] { Destinatario }));
            Assert.That(riletto.Bcc, Is.Empty);
            Assert.That(riletto.Cc, Is.Empty);
        });
    }

    /// <summary>Il corpo e' trasportato come testo: markup, script e sequenze MIME non vengono interpretati.</summary>
    [TestCase("<script>alert(1)</script>")]
    [TestCase("'; DROP TABLE Email; --")]
    [TestCase("--boundary\r\nContent-Type: text/plain\r\n\r\nparte iniettata")]
    [TestCase("\r\n.\r\nMAIL FROM:<intruso@portalefatture.invalid>")]
    public void Componi_CorpoOstile_ShouldArrivareIdenticoEInUnaSolaParte(string corpo)
    {
        var riletto = GiroCompleto(Componi(corpo: corpo));

        Assert.Multiple(() =>
        {
            Assert.That(Corpo(riletto).ReplaceLineEndings("\n"), Is.EqualTo(corpo.ReplaceLineEndings("\n")));
            Assert.That(riletto.BodyParts.Count(), Is.EqualTo(1), "una sola parte MIME");
            Assert.That(riletto.To.Mailboxes.Select(m => m.Address), Is.EqualTo(new[] { Destinatario }));
        });
    }

    [Test]
    public void Componi_CorpoMoltoGrande_ShouldSopravvivereAlGiroCompleto()
    {
        var corpo = new StringBuilder().Insert(0, "<p>riga di un report con importo 1.234,56 €</p>", 20000).ToString();

        var riletto = GiroCompleto(Componi(corpo: corpo));

        Assert.That(Corpo(riletto), Is.EqualTo(corpo));
    }

    [Test]
    public void Componi_OggettoMoltoLungo_ShouldEssereRipiegatoMaRilettoIntero()
    {
        var oggetto = string.Join(" ", Enumerable.Repeat("Sollecito fattura n. 2026/000123 —", 40));

        var riletto = GiroCompleto(Componi(oggetto: oggetto));

        Assert.That(riletto.Subject, Is.EqualTo(oggetto));
    }

    /// <summary>
    /// Destinatario o mittente assenti: la composizione si ferma con un'eccezione, che i sender
    /// trasformano in (messaggio, false). Nessun messaggio senza destinatario arriva a Google.
    /// </summary>
    [Test]
    public void Componi_IndirizzoAssente_ShouldLanciare()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Componi(destinatario: null), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => Componi(mittente: null), Throws.InstanceOf<ArgumentNullException>());
        });
    }

    /// <summary>
    /// Piu' indirizzi nello stesso campo: la composizione li rifiuta (ParseException), quindi non
    /// possono diventare destinatari aggiuntivi. I sender restituiscono (messaggio, false).
    /// </summary>
    [TestCase("uno@portalefatture.invalid, due@portalefatture.invalid")]
    [TestCase("uno@portalefatture.invalid; due@portalefatture.invalid")]
    [TestCase("uno@portalefatture.invalid\r\nBcc: due@portalefatture.invalid")]
    public void Componi_PiuIndirizziNelDestinatario_ShouldEssereRifiutati(string indirizzo)
    {
        Assert.That(() => GiroCompleto(Componi(destinatario: indirizzo)), Throws.InstanceOf<ParseException>());
    }
}
