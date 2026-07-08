namespace PortaleFatture.BE.IntegrationTest;

[SetUpFixture]
public sealed class NoEmailGuardrailsSetup
{
    private static readonly string[] AllowedNonProductionEnvironments = ["fat-d-api-func", "fat-u-api-func", "debug"];

    [OneTimeSetUp]
    public void EnforceSafeEnvironment()
    {
        Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "debug", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("PortaleFattureOptions:WEBSITE_SITE_NAME", "debug", EnvironmentVariableTarget.Process);

        ClearSensitiveMailVariables();
    }

    [OneTimeTearDown]
    public void CleanupSafeEnvironment()
    {
        Environment.SetEnvironmentVariable("PortaleFattureOptions:WEBSITE_SITE_NAME", null, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", null, EnvironmentVariableTarget.Process);
    }

    private static void ClearSensitiveMailVariables()
    {
        string[] keys =
        [
            "PortaleFattureOptions:AccessToken",
            "PortaleFattureOptions:RefreshToken",
            "PortaleFattureOptions:ClientId",
            "PortaleFattureOptions:ClientSecret",
            "ACCESSTOKEN",
            "REFRESHTOKEN",
            "CLIENTID",
            "CLIENTSECRET"
        ];

        foreach (var key in keys)
        {
            Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.Process);
        }
    }

    public static bool IsSafeEnvironment(string? environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            return false;
        }

        return AllowedNonProductionEnvironments.Contains(environment.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}

public class NoEmailGuardrailsTests
{
    [Test]
    public void Environment_ShouldBeForcedToNonProductionValue()
    {
        var env = Environment.GetEnvironmentVariable("PortaleFattureOptions:WEBSITE_SITE_NAME", EnvironmentVariableTarget.Process)
                  ?? Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME", EnvironmentVariableTarget.Process);

        Assert.That(NoEmailGuardrailsSetup.IsSafeEnvironment(env), Is.True,
            "Unsafe environment detected for tests. Abort to prevent any outbound email attempts.");
    }
}
