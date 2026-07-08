using NUnit.Framework.Legacy;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

namespace PortaleFatture.BE.UnitTest;

public class DocumentPspBuilderTests
{
    [Test]
    public void CreateEmailAdjsutmentHtml_ShouldUseAdjustmentTemplateFile()
    {
        var root = CreateTemplateRoot(
            financialTemplate: "Financial template",
            adjustmentTemplate: "Adjustment template [RagioneSociale]");

        var builder = new DocumentPspBuilder(root);

        var html = builder.CreateEmailAdjsutmentHtml(new PspEmail
        {
            RagioneSociale = "PSP Adjust"
        });

        ClassicAssert.IsNotNull(html);
        Assert.That(html!, Does.Contain("Adjustment template PSP Adjust"));
        Assert.That(html, Does.Not.Contain("Financial template"));
    }

    [Test]
    public void CreateEmailAdjsutmentHtml_RealTemplate_ShouldContainOfficialAdjustmentMessageAndLinks()
    {
        var solutionRoot = FindSolutionRoot();
        var functionRoot = Path.Combine(solutionRoot, "src", "Presentation", "PortaleFatture.BE.SendEmailFunction");

        var builder = new DocumentPspBuilder(functionRoot);

        var html = builder.CreateEmailAdjsutmentHtml(new PspEmail
        {
            Anno = 2026,
            Trimestre = "2026_1",
            IdContratto = "C_ADJ",
            Tipologia = "FINANCIAL_ADJUST",
            Email = "unit-test@pagopa.it",
            RagioneSociale = "PSP Adjustment",
            DetailReport = "https://example.test/detail",
            AgentReport = "https://example.test/agent"
        });

        ClassicAssert.IsNotNull(html);
        Assert.That(html!, Does.Contain("Spettabile Prestatore Aderente"));
        Assert.That(html, Does.Contain("piattaforma_pagoPA: Informativa su fatturazione primo trimestre"));
        Assert.That(html, Does.Contain("Download Detail Report"));
        Assert.That(html, Does.Contain("Download Agent Quarter Report"));
        Assert.That(html, Does.Contain("https://example.test/detail"));
        Assert.That(html, Does.Contain("https://example.test/agent"));
    }

    [Test]
    public void CreateEmailHtml_WhenTemplateMissing_ShouldThrowConfigurationException()
    {
        var root = Path.Combine(Path.GetTempPath(), "pfw-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Infrastructure", "Documenti", "pagoPA"));
        var builder = new DocumentPspBuilder(root);

        Assert.Throws<ConfigurationException>(() => builder.CreateEmailHtml(new PspEmail()));
    }

    private static string CreateTemplateRoot(string financialTemplate, string adjustmentTemplate)
    {
        var root = Path.Combine(Path.GetTempPath(), "pfw-tests", Guid.NewGuid().ToString("N"));
        var templateDir = Path.Combine(root, "Infrastructure", "Documenti", "pagoPA");
        Directory.CreateDirectory(templateDir);

        File.WriteAllText(Path.Combine(templateDir, "email_psp.html"), financialTemplate);
        File.WriteAllText(Path.Combine(templateDir, "email_psp_adjustment.html"), adjustmentTemplate);

        return root;
    }

    private static string FindSolutionRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            var slnPath = Path.Combine(current.FullName, "PortaleFatture.BE.Api.sln");
            if (File.Exists(slnPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate solution root from test directory.");
    }
}
