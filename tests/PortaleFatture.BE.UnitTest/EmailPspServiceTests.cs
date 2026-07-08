using NUnit.Framework.Legacy;
using PortaleFatture.BE.Core.Entities.pagoPA.AnagraficaPSP;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Services;
using System.Reflection;

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
            Trimestre = "2026_1",
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
            Trimestre = "2026_1",
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

        var result = service.CountInvio("2026_1");

        ClassicAssert.IsFalse(result);
    }

    [Test]
    public void CountInvioAdjustment_WithInvalidConnection_ShouldReturnFalse()
    {
        var service = new EmailPspService(InvalidConnectionString);

        var result = service.CountInvioAdjustment("2026_1");

        ClassicAssert.IsFalse(result);
    }

    [Test]
    public void GetSenderEmail_WithInvalidConnection_ShouldReturnEmpty()
    {
        var service = new EmailPspService(InvalidConnectionString);

        var result = service.GetSenderEmail("2026_1");

        ClassicAssert.IsNotNull(result);
        ClassicAssert.IsEmpty(result!);
    }

    [Test]
    public void GetSenderEmailAdjustment_WithInvalidConnection_ShouldReturnEmpty()
    {
        var service = new EmailPspService(InvalidConnectionString);

        var result = service.GetSenderEmailAdjustment("2026_1");

        ClassicAssert.IsNotNull(result);
        ClassicAssert.IsEmpty(result!);
    }

    [Test]
    public void GetSenderEmailReinvio_WithInvalidConnection_ShouldReturnEmpty()
    {
        var service = new EmailPspService(InvalidConnectionString);

        var result = service.GetSenderEmailReinvio("2026_1");

        ClassicAssert.IsNotNull(result);
        ClassicAssert.IsEmpty(result);
    }

    [Test]
    public void SqlSelect_ShouldContainFallbackFromReferenteToCourtesyMail()
    {
        var service = new EmailPspService(InvalidConnectionString);

        var field = typeof(EmailPspService).GetField("_sqlSelect", BindingFlags.NonPublic | BindingFlags.Instance);
        ClassicAssert.IsNotNull(field, "Private field _sqlSelect not found.");

        var sql = field!.GetValue(service) as string;
        ClassicAssert.IsNotNull(sql);

        ClassicAssert.IsTrue(sql!.Contains("ISNULL(NULLIF([referentefattura_mail], ''), [courtesy_mail]) as email", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void SqlSelect_ShouldPrioritizeReferenteBeforeCourtesyMail()
    {
        var service = new EmailPspService(InvalidConnectionString);

        var field = typeof(EmailPspService).GetField("_sqlSelect", BindingFlags.NonPublic | BindingFlags.Instance);
        ClassicAssert.IsNotNull(field, "Private field _sqlSelect not found.");

        var sql = field!.GetValue(service) as string;
        ClassicAssert.IsNotNull(sql);

        var referIndex = sql!.IndexOf("[referentefattura_mail]", StringComparison.OrdinalIgnoreCase);
        var courtesyIndex = sql.IndexOf("[courtesy_mail]", StringComparison.OrdinalIgnoreCase);

        ClassicAssert.IsTrue(referIndex >= 0, "[referentefattura_mail] not found in _sqlSelect.");
        ClassicAssert.IsTrue(courtesyIndex >= 0, "[courtesy_mail] not found in _sqlSelect.");
        ClassicAssert.IsTrue(referIndex < courtesyIndex, "Expected referentefattura_mail to be evaluated before courtesy_mail.");
    }

    [Test]
    public void SqlCountInvioEmailAdjust_ShouldFilterOnFinancialAdjustTipologia()
    {
        var service = new EmailPspService(InvalidConnectionString);

        var field = typeof(EmailPspService).GetField("_sqlCountInvioEmailAdjust", BindingFlags.NonPublic | BindingFlags.Instance);
        ClassicAssert.IsNotNull(field, "Private field _sqlCountInvioEmailAdjust not found.");

        var sql = field!.GetValue(service) as string;
        ClassicAssert.IsNotNull(sql);
        ClassicAssert.IsTrue(sql!.Contains("tipologia = 'FINANCIAL_ADJUST'", StringComparison.OrdinalIgnoreCase));
    }
}
