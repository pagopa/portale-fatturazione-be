using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NUnit.Framework.Legacy;
using PortaleFatture.BE.Core.Entities.pagoPA.AnagraficaPSP;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Services;

namespace PortaleFatture.BE.IntegrationTest;

public class EmailPspServiceIntegrationTests
{
    private IConfiguration _conf;

    [SetUp]
    public void Setup()
    {
        _conf = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<EmailPspServiceIntegrationTests>()
            .AddEnvironmentVariables()
            .Build();
    }

    [Explicit]
    [Test]
    public void InsertPreviewEmail_ShouldPersistInStgPreviewTable()
    {
        var connectionString = Required("PortaleFattureOptions:ConnectionString");
        var service = new EmailPspService(connectionString);

        var idContratto = $"IT_PREV_{Guid.NewGuid():N}";
        var trimestre = "2026_1";
        var data = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        try
        {
            var inserted = service.InsertPreviewEmail(new PspEmailTracking
            {
                IdContratto = idContratto,
                Tipologia = EmailPspTipologia.Financial,
                Anno = 2026,
                Trimestre = trimestre,
                Data = data,
                Email = "integration-test@pagopa.it",
                Oggetto = "Preview Oggetto",
                Corpo = "Preview Corpo",
                Link = "https://example.test/preview",
                RagioneSociale = "Integration Test PSP",
                Invio = 0,
                TipoContratto = "TEST"
            });

            ClassicAssert.IsTrue(inserted, "InsertPreviewEmail returned false");

            var exists = ExistsInPreview(connectionString, idContratto, trimestre);
            ClassicAssert.IsTrue(exists, "Inserted row not found in [stg].[PspEmailPreview]");
        }
        finally
        {
            CleanupPreview(connectionString, idContratto, trimestre);
        }
    }

    [Explicit]
    [Test]
    public void InsertTracciatoEmail_ShouldPersistInPpaPspEmail_WithOptionalFields()
    {
        var connectionString = Required("PortaleFattureOptions:ConnectionString");
        var service = new EmailPspService(connectionString);

        var idContratto = $"IT_TRK_{Guid.NewGuid():N}";
        var trimestre = "2026_1";
        var data = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        try
        {
            var inserted = service.InsertTracciatoEmail(new PspEmailTracking
            {
                IdContratto = idContratto,
                Tipologia = EmailPspTipologia.Financial,
                Anno = 2026,
                Trimestre = trimestre,
                Data = data,
                Email = "integration-test@pagopa.it",
                Messaggio = "Integration test message",
                Oggetto = "Tracking Oggetto",
                Corpo = "Tracking Corpo",
                Link = "https://example.test/tracking",
                RagioneSociale = "Integration Test PSP",
                Invio = 0
            });

            ClassicAssert.IsTrue(inserted, "InsertTracciatoEmail returned false");

            var row = GetTrackingRow(connectionString, idContratto, trimestre);
            ClassicAssert.IsNotNull(row, "Inserted row not found in [ppa].[PspEmail]");
            Assert.That(row!.Oggetto, Is.EqualTo("Tracking Oggetto"));
            Assert.That(row.Corpo, Is.EqualTo("Tracking Corpo"));
            Assert.That(row.Link, Is.EqualTo("https://example.test/tracking"));
        }
        finally
        {
            CleanupTracking(connectionString, idContratto, trimestre);
        }
    }

    [Test]
    public void GetSenderEmail_ReadOnly_ShouldUseReferenteMail_WhenNotEmpty()
    {
        var connectionString = Required("PortaleFattureOptions:ConnectionString");
        var service = new EmailPspService(connectionString);
        const string quarter = "2026_1";

        var result = service.GetSenderEmail(quarter);

        ClassicAssert.IsNotNull(result);
        var expected = GetExpectedRowWithReferente(connectionString, quarter);
        if (expected is null)
        {
            Assert.Pass($"No suitable referente record found for quarter {quarter}; read-only query executed successfully.");
            return;
        }

        var row = result!.FirstOrDefault(x => x.IdContratto == expected.IdContratto);

        ClassicAssert.IsNotNull(row, $"Contract {expected.IdContratto} not found in GetSenderEmail result.");
        Assert.That(row!.Email, Is.EqualTo(expected.ExpectedEmail));
        Assert.That(row.RagioneSociale, Is.EqualTo(expected.RagioneSociale));
        Assert.That(row.AgentReport, Is.EqualTo(ParseReportLink(expected.Links, "agentquarter")));
        Assert.That(row.DetailReport, Is.EqualTo(ParseReportLink(expected.Links, "detailed")));
        Assert.That(row.DiscountReport, Is.EqualTo(expected.DiscountReport ?? string.Empty));
    }

    [Test]
    public void GetSenderEmail_ReadOnly_ShouldFallbackToCourtesyMail_WhenReferenteEmpty()
    {
        var connectionString = Required("PortaleFattureOptions:ConnectionString");
        var service = new EmailPspService(connectionString);
        const string quarter = "2026_1";

        var result = service.GetSenderEmail(quarter);

        ClassicAssert.IsNotNull(result);
        var expected = GetExpectedRowWithCourtesyFallback(connectionString, quarter);
        if (expected is null)
        {
            Assert.Pass($"No suitable courtesy fallback record found for quarter {quarter}; read-only query executed successfully.");
            return;
        }

        var fallbackRow = result!.FirstOrDefault(x => x.IdContratto == expected.IdContratto);

        ClassicAssert.IsNotNull(fallbackRow, $"Contract {expected.IdContratto} not found in GetSenderEmail result.");
        Assert.That(fallbackRow!.Email, Is.EqualTo(expected.ExpectedEmail));
        Assert.That(fallbackRow.RagioneSociale, Is.EqualTo(expected.RagioneSociale));
        Assert.That(fallbackRow.AgentReport, Is.EqualTo(ParseReportLink(expected.Links, "agentquarter")));
        Assert.That(fallbackRow.DetailReport, Is.EqualTo(ParseReportLink(expected.Links, "detailed")));
        Assert.That(fallbackRow.DiscountReport, Is.EqualTo(expected.DiscountReport ?? string.Empty), "DiscountReport should be empty when percsconto <= 0.");

        Assert.That(result.Any(x => x.Trimestre != quarter), Is.False,
            "Quarter filter regression: result contains records from a different quarter.");
    }

    [Test]
    public void GetSenderEmailAdjustment_ReadOnly_ShouldReturnAdjustmentTipologiaAndQuarterFilter()
    {
        var connectionString = Required("PortaleFattureOptions:ConnectionString");
        var service = new EmailPspService(connectionString);
        const string quarter = "2026_1";

        var result = service.GetSenderEmailAdjustment(quarter);

        ClassicAssert.IsNotNull(result);

        var first = result!.FirstOrDefault();
        if (first is null)
        {
            Assert.Pass($"No adjustment sender records found for quarter {quarter}; read-only query executed successfully.");
            return;
        }

        Assert.That(first.Tipologia, Is.EqualTo(EmailPspTipologia.FinancialAdjust));
        Assert.That(result.All(x => x.Trimestre == quarter), Is.True,
            "Quarter filter regression: adjustment result contains records from a different quarter.");
    }

    [Test]
    public void CountInvioAdjustment_ReadOnly_ShouldMatchDatabaseCountEvaluation()
    {
        var connectionString = Required("PortaleFattureOptions:ConnectionString");
        var service = new EmailPspService(connectionString);
        const string quarter = "2026_1";

        var serviceResult = service.CountInvioAdjustment(quarter);
        var dbCount = GetAdjustmentCount(connectionString, quarter);

        Assert.That(serviceResult, Is.EqualTo(dbCount > 0));
    }

    private string Required(string key)
    {
        var value = _conf.GetValue<string>(key);
        ClassicAssert.IsFalse(string.IsNullOrWhiteSpace(value), $"{key} not configured (User Secrets)");
        return value!;
    }

    private static ExpectedSenderRow? GetExpectedRowWithReferente(string connectionString, string quarter)
    {
        const string sql = @"
SELECT TOP 1
    c.contract_id,
    c.name,
    c.referentefattura_mail,
    k.descrizione_riga,
    s.linkReport
FROM [ppa].[kpmg] k
INNER JOIN [ppa].[Contracts] c
    ON c.contract_id = k.contract_id
   AND c.year_quarter = k.year_quarter
OUTER APPLY (
    SELECT TOP 1 ks.linkReport
    FROM [ppa].[KpiPagamenti_Sconto] ks
    WHERE ks.recipient_id = k.contract_id
      AND ks.year_quarter = k.year_quarter
      AND ks.percsconto > 0
    ORDER BY ks.linkReport
) s
WHERE k.year_quarter = @quarter
  AND LEN(ISNULL(k.descrizione_riga, '')) > 0
  AND LEN(LTRIM(RTRIM(ISNULL(c.referentefattura_mail, '')))) > 0
ORDER BY c.contract_id;";

        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@quarter", quarter);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new ExpectedSenderRow(
            IdContratto: reader.GetString(0),
            RagioneSociale: reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            ExpectedEmail: reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            Links: reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            DiscountReport: reader.IsDBNull(4) ? null : reader.GetString(4));
    }

    private static ExpectedSenderRow? GetExpectedRowWithCourtesyFallback(string connectionString, string quarter)
    {
        const string sql = @"
SELECT TOP 1
    c.contract_id,
    c.name,
    c.courtesy_mail,
    k.descrizione_riga,
    s.linkReport
FROM [ppa].[kpmg] k
INNER JOIN [ppa].[Contracts] c
    ON c.contract_id = k.contract_id
   AND c.year_quarter = k.year_quarter
OUTER APPLY (
    SELECT TOP 1 ks.linkReport
    FROM [ppa].[KpiPagamenti_Sconto] ks
    WHERE ks.recipient_id = k.contract_id
      AND ks.year_quarter = k.year_quarter
      AND ks.percsconto > 0
    ORDER BY ks.linkReport
) s
WHERE k.year_quarter = @quarter
  AND LEN(ISNULL(k.descrizione_riga, '')) > 0
  AND LEN(LTRIM(RTRIM(ISNULL(c.referentefattura_mail, '')))) = 0
  AND LEN(LTRIM(RTRIM(ISNULL(c.courtesy_mail, '')))) > 0
ORDER BY c.contract_id;";

        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@quarter", quarter);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new ExpectedSenderRow(
            IdContratto: reader.GetString(0),
            RagioneSociale: reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            ExpectedEmail: reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            Links: reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            DiscountReport: reader.IsDBNull(4) ? null : reader.GetString(4));
    }

    private static string? ParseReportLink(string links, string token)
    {
        if (string.IsNullOrWhiteSpace(links))
        {
            return null;
        }

        return links
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(x => x.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ExistsInPreview(string connectionString, string idContratto, string trimestre)
    {
        const string sql = @"
SELECT COUNT(1)
FROM [stg].[PspEmailPreview]
WHERE [IdContratto] = @IdContratto
  AND [Trimestre] = @Trimestre;";

        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@IdContratto", idContratto);
        cmd.Parameters.AddWithValue("@Trimestre", trimestre);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static TrackingRow? GetTrackingRow(string connectionString, string idContratto, string trimestre)
    {
        const string sql = @"
SELECT TOP 1 [Oggetto], [Corpo], [Link]
FROM [ppa].[PspEmail]
WHERE [IdContratto] = @IdContratto
  AND [Trimestre] = @Trimestre
ORDER BY [DataEvento] DESC;";

        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@IdContratto", idContratto);
        cmd.Parameters.AddWithValue("@Trimestre", trimestre);
        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return new TrackingRow(
            reader.IsDBNull(0) ? null : reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2));
    }

    private static void CleanupPreview(string connectionString, string idContratto, string trimestre)
    {
        const string sql = @"
DELETE FROM [stg].[PspEmailPreview]
WHERE [IdContratto] = @IdContratto
  AND [Trimestre] = @Trimestre;";

        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@IdContratto", idContratto);
        cmd.Parameters.AddWithValue("@Trimestre", trimestre);
        cmd.ExecuteNonQuery();
    }

    private static void CleanupTracking(string connectionString, string idContratto, string trimestre)
    {
        const string sql = @"
DELETE FROM [ppa].[PspEmail]
WHERE [IdContratto] = @IdContratto
  AND [Trimestre] = @Trimestre;";

        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@IdContratto", idContratto);
        cmd.Parameters.AddWithValue("@Trimestre", trimestre);
        cmd.ExecuteNonQuery();
    }

    private static int GetAdjustmentCount(string connectionString, string quarter)
    {
        const string sql = @"
SELECT COUNT(1)
FROM [ppa].[PspEmail]
WHERE [Trimestre] = @Trimestre
  AND [Tipologia] = 'FINANCIAL_ADJUST';";

        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Trimestre", quarter);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private sealed record TrackingRow(string? Oggetto, string? Corpo, string? Link);

    private sealed record ExpectedSenderRow(
        string IdContratto,
        string RagioneSociale,
        string ExpectedEmail,
        string Links,
        string? DiscountReport);
}
