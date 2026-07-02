using NUnit.Framework.Legacy;
using PortaleFatture.BE.Core.Entities.pagoPA.AnagraficaPSP;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Services;

namespace PortaleFatture.BE.UnitTest;

public class EmailPspServiceTests
{
    private const string InvalidConnectionString =
        "Server=localhost,65000;Database=not_exists;User Id=sa;Password=WrongPassword!;TrustServerCertificate=True;Connection Timeout=1;";

    [Test]
    public void InsertPreviewEmail_WithInvalidConnection_ShouldReturnFalse()
    {
        var service = new EmailPspService(InvalidConnectionString);

        var result = service.InsertPreviewEmail(new PspEmailTracking
        {
            IdContratto = "UT_PREVIEW",
            Tipologia = EmailPspTipologia.Financial,
            Anno = 2026,
            Trimestre = "2026_Q3",
            Data = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            Email = "unit-test@pagopa.it",
            Oggetto = "subject",
            Corpo = "body",
            Link = "https://example.test/detail",
            RagioneSociale = "Unit Test",
            Invio = 0,
            TipoContratto = "TEST"
        });

        ClassicAssert.IsFalse(result);
    }

    [Test]
    public void InsertTracciatoEmail_WithInvalidConnection_ShouldReturnFalse()
    {
        var service = new EmailPspService(InvalidConnectionString);

        var result = service.InsertTracciatoEmail(new PspEmailTracking
        {
            IdContratto = "UT_TRACK",
            Tipologia = EmailPspTipologia.Financial,
            Anno = 2026,
            Trimestre = "2026_Q3",
            Data = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            Email = "unit-test@pagopa.it",
            Messaggio = "message",
            Oggetto = "subject",
            Corpo = "body",
            Link = "https://example.test/detail",
            RagioneSociale = "Unit Test",
            Invio = 0
        });

        ClassicAssert.IsFalse(result);
    }

    [Test]
    public void CountInvio_WithInvalidConnection_ShouldReturnFalse()
    {
        var service = new EmailPspService(InvalidConnectionString);

        var result = service.CountInvio("2026_Q3");

        ClassicAssert.IsFalse(result);
    }

    [Test]
    public void GetSenderEmail_WithInvalidConnection_ShouldReturnEmpty()
    {
        var service = new EmailPspService(InvalidConnectionString);

        var result = service.GetSenderEmail("2026_Q3");

        ClassicAssert.IsNotNull(result);
        ClassicAssert.IsEmpty(result!);
    }

    [Test]
    public void GetSenderEmailReinvio_WithInvalidConnection_ShouldReturnEmpty()
    {
        var service = new EmailPspService(InvalidConnectionString);

        var result = service.GetSenderEmailReinvio("2026_Q3");

        ClassicAssert.IsNotNull(result);
        ClassicAssert.IsEmpty(result);
    }
}
