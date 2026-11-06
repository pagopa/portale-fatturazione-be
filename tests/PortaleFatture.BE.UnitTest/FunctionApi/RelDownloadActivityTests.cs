using System.Text;
using Azure.Storage.Blobs.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Models;
using PortaleFatture.BE.Function.API.RegolareEsecuzione;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Services;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;

namespace PortaleFatture.BE.UnitTest.FunctionApi;

/// <summary>
/// Activity <see cref="RelDownload"/> della Integration API (<c>/v1/rel/download</c>): sceglie se la
/// REL e' scaricabile, genera il documento con <see cref="DocumentBuilder"/> (template veri,
/// dall'output della build) e lo carica sullo storage restituendo il SAS.
///
/// IMediator e storage sono finti: si verifica la logica dell'activity e il documento che finirebbe
/// sul blob, non la query (coperta dagli integration test su <c>vwRelDettaglio</c>).
///
/// ⚠️ SEM. SOSPESI: il filtro dell'activity esclude solo var/semestrale/annuale, quindi SEM. SOSPESI
/// passa e, dalla 2.63.6 (PF-997), il documento viene generato. Se debba restare esposta ai grandi
/// aderenti e' una **decisione di prodotto aperta** (docs/integration-api-riferimento.md): il test
/// fissa il comportamento del codice di oggi; se si decide di escluderla, va invertito.
/// </summary>
[TestFixture]
public class RelDownloadActivityTests
{
    private const string Ente = "11111111-1111-1111-1111-111111111111";
    private const string AltroEnte = "22222222-2222-2222-2222-222222222222";

    private Mock<IMediator> _mediator = null!;
    private Mock<IBlobStorageRelDownload> _storage = null!;
    private byte[]? _caricato;
    private string? _mimeCaricato;

    [SetUp]
    public void SetUp()
    {
        _mediator = new Mock<IMediator>();
        _storage = new Mock<IBlobStorageRelDownload>();
        _caricato = null;
        _mimeCaricato = null;

        _storage
            .Setup(s => s.UploadStreamAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<Stream, string, int?, int?, string, string, string>((stream, _, _, _, _, _, mime) =>
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                _caricato = ms.ToArray();
                _mimeCaricato = mime;
            })
            .ReturnsAsync(Mock.Of<BlobContentInfo>());
        _storage
            .Setup(s => s.GetSasToken(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("https://storage.test/rel?sas");
    }

    private void RelNelDb(string? tipologia) =>
        _mediator
            .Setup(m => m.Send(It.IsAny<RelTestataQueryGetById>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tipologia is null ? null : new RelTestataDettaglioDto
            {
                IdEnte = Ente,
                IdContratto = "TOKEN-E1",
                TipologiaFattura = tipologia,
                Anno = "2026",
                Mese = "5",
                RagioneSociale = "Comune di Prova",
                Totale = 1234.56m,
                TotaleAnalogico = 1000m,
                TotaleDigitale = 234.56m,
                TotaleNotificheAnalogiche = 70,
                TotaleNotificheDigitali = 30,
            });

    private Task<RELDownloadResponse> Esegui(string tipologiaChiave, string? tipoDocumento = "html", string enteSessione = Ente)
    {
        var services = new ServiceCollection()
            .AddSingleton(_mediator.Object)
            .AddSingleton<IDocumentBuilder>(new DocumentBuilder(TestContext.CurrentContext.TestDirectory))
            .AddSingleton(_storage.Object)
            .BuildServiceProvider();

        var context = new FakeFunctionContext { InstanceServices = services };
        var request = new RELDownloadInternalRequest
        {
            IdTestata = $"{Ente}_TOKEN-E1_{tipologiaChiave.Replace(' ', '-')}_2026_5",
            TypeDocument = tipoDocumento,
            Session = new Session { IdEnte = enteSessione, Profilo = "PA" },
        };

        return new RelDownload(NullLoggerFactory.Instance).RunAsync(request, context);
    }

    [TestCase("PRIMO SALDO")]
    [TestCase("SECONDO SALDO")]
    [TestCase("SEM. SOSPESI")] // PF-997 — v. avviso sulla classe
    public async Task RelConTemplate_Html_CaricaIlDocumentoERestituisceIlLink(string tipologia)
    {
        RelNelDb(tipologia);

        var risposta = await Esegui(tipologia);

        Assert.Multiple(() =>
        {
            Assert.That(risposta.LinkDocumento, Is.EqualTo("https://storage.test/rel?sas"));
            Assert.That(_mimeCaricato, Is.EqualTo("text/html"));
            var html = Encoding.UTF8.GetString(_caricato!);
            Assert.That(html, Does.Contain("Comune di Prova").And.Contain("TOKEN-E1"));
            Assert.That(html, Does.Not.Contain("[RagioneSociale]").And.Not.Contain("[Totale]"));
        });
    }

    [Test]
    public async Task SemSospesi_Pdf_CaricaUnPdf()
    {
        RelNelDb("SEM. SOSPESI");

        try
        {
            await Esegui("SEM. SOSPESI", tipoDocumento: "pdf");
        }
        catch (Exception ex) when (ex is DllNotFoundException || ex.InnerException is DllNotFoundException)
        {
            Assert.Inconclusive($"Librerie native PDF non disponibili su questa macchina: {ex.Message}");
        }

        Assert.Multiple(() =>
        {
            Assert.That(_mimeCaricato, Is.EqualTo("application/pdf"));
            Assert.That(Encoding.ASCII.GetString(_caricato!, 0, 5), Is.EqualTo("%PDF-"));
        });
    }

    [TestCase("PDF")]   // confronto case-sensitive con TipoDocumentoREL.PDF
    [TestCase(null)]
    [TestCase("docx")]
    public async Task TipoDocumentoNonPdf_GeneraHtml(string? tipo)
    {
        RelNelDb("PRIMO SALDO");

        await Esegui("PRIMO SALDO", tipoDocumento: tipo);

        Assert.That(_mimeCaricato, Is.EqualTo("text/html"));
    }

    [TestCase("VAR. SEMESTRALE")]
    [TestCase("VAR. ANNUALE")]
    public void Variazioni_NonScaricabili_NotFoundSenzaGenerareNulla(string tipologia)
    {
        RelNelDb(tipologia);

        Assert.That(async () => await Esegui(tipologia),
            Throws.TypeOf<DomainException>().With.Message.EqualTo(MessageHelper.NotFound));
        _storage.Verify(s => s.UploadStreamAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void RelInesistente_NotFound()
    {
        RelNelDb(null);

        Assert.That(async () => await Esegui("PRIMO SALDO"),
            Throws.TypeOf<DomainException>().With.Message.EqualTo(MessageHelper.NotFound));
    }

    [TestCase("ACCONTO")]
    [TestCase("ASSEVERAZIONE")]
    public void TipologiaSenzaTemplateNonFiltrata_EccezioneDelBuilder_Caratterizzazione(string tipologia)
    {
        // Il filtro dell'activity e' per sottostringa e non conosce l'elenco dei template: una REL
        // di tipologia senza template (oggi non ne esistono in vwRelDettaglio) arriverebbe al
        // builder, che lancia un'eccezione generica invece del NotFound. Era il caso di SEM. SOSPESI
        // prima della 2.63.6.
        RelNelDb(tipologia);

        Assert.That(async () => await Esegui(tipologia),
            Throws.Exception.With.Message.EqualTo("Tipologia fattura non valida"));
    }

    [Test]
    public void ChiaveDiUnAltroEnte_WrongIdEnte_SenzaInterrogareIlDb()
    {
        RelNelDb("PRIMO SALDO");

        Assert.That(async () => await Esegui("PRIMO SALDO", enteSessione: AltroEnte),
            Throws.TypeOf<DomainException>().With.Message.EqualTo("Wrong Id Ente"));
        _mediator.Verify(m => m.Send(It.IsAny<RelTestataQueryGetById>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task LaQueryUsaLEnteDellaSessione()
    {
        RelNelDb("SEM. SOSPESI");
        RelTestataQueryGetById? inviata = null;
        _mediator
            .Setup(m => m.Send(It.IsAny<RelTestataQueryGetById>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<RelTestataDettaglioDto?>, CancellationToken>((q, _) => inviata = (RelTestataQueryGetById)q)
            .ReturnsAsync(new RelTestataDettaglioDto
            {
                IdEnte = Ente, IdContratto = "TOKEN-E1", TipologiaFattura = "SEM. SOSPESI", Anno = "2026", Mese = "5",
                RagioneSociale = "Comune di Prova", Totale = 1m, TotaleAnalogico = 1m, TotaleDigitale = 0m,
            });

        await Esegui("SEM. SOSPESI");

        Assert.Multiple(() =>
        {
            Assert.That(inviata, Is.Not.Null);
            Assert.That(inviata!.AuthenticationInfo.IdEnte, Is.EqualTo(Ente));
            Assert.That(inviata.IdTestata, Is.EqualTo($"{Ente}_TOKEN-E1_SEM.-SOSPESI_2026_5"));
        });
    }
}
