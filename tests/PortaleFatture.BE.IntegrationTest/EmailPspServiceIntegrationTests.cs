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
        var trimestre = "2026_Q3";
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
        var trimestre = "2026_Q3";
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
            ClassicAssert.AreEqual("Tracking Oggetto", row!.Oggetto);
            ClassicAssert.AreEqual("Tracking Corpo", row.Corpo);
            ClassicAssert.AreEqual("https://example.test/tracking", row.Link);
        }
        finally
        {
            CleanupTracking(connectionString, idContratto, trimestre);
        }
    }

    private string Required(string key)
    {
        var value = _conf.GetValue<string>(key);
        ClassicAssert.IsFalse(string.IsNullOrWhiteSpace(value), $"{key} not configured (User Secrets)");
        return value!;
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

    private sealed record TrackingRow(string? Oggetto, string? Corpo, string? Link);
}
