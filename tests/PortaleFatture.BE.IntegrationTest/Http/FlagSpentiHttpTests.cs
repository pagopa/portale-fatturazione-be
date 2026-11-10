using Microsoft.Extensions.DependencyInjection;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.IntegrationTest.Http;

// Feature flag (PF-550, PF-735): le rotte FUORI dai flag devono continuare a funzionare anche con tutti i
// flag spenti. Ogni classe qui sotto rilancia, ereditandoli, TUTTI i test HTTP di una suite fuori dai flag,
// con i flag spenti: li spegne la proprieta' ApiTestFactory.ProprietaFlagSpenti, letta dalla factory.
// Restano escluse le suite delle rotte sotto flag (AppIo*, InvioSap*, FattureInvioSapPosticipate, che usa
// le rotte legacy) e quelle dei flag stessi. Una suite HTTP nuova fuori dai flag va aggiunta qui: lo
// ricorda FlagSpentiCoperturaTests.

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class ApiKeysHttpTestsConFlagSpenti : ApiKeysHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class FatturaEmessaDocumentoDownloadHttpTestsConFlagSpenti : FatturaEmessaDocumentoDownloadHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class FatturaSospesaDocumentoDownloadHttpTestsConFlagSpenti : FatturaSospesaDocumentoDownloadHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class FattureReportNonFatturateHttpTestsConFlagSpenti : FattureReportNonFatturateHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class FattureRicercaHttpTestsConFlagSpenti : FattureRicercaHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class FattureWhiteListHttpTestsConFlagSpenti : FattureWhiteListHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFattureAnnullaHttpTestsConFlagSpenti : GestioneFattureAnnullaHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFattureDownloadHttpTestsConFlagSpenti : GestioneFattureDownloadHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFattureEliminaBodyRealeHttpTestsConFlagSpenti : GestioneFattureEliminaBodyRealeHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFattureEliminaDalFormHttpTestsConFlagSpenti : GestioneFattureEliminaDalFormHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFattureEliminaDallaRigaHttpTestsConFlagSpenti : GestioneFattureEliminaDallaRigaHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFattureEliminaEccezioneInpsHttpTestsConFlagSpenti : GestioneFattureEliminaEccezioneInpsHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFattureFormAggiungiHttpTestsConFlagSpenti : GestioneFattureFormAggiungiHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFattureGrigliaAdversarialHttpTestsConFlagSpenti : GestioneFattureGrigliaAdversarialHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFattureGrigliaFiltriHttpTestsConFlagSpenti : GestioneFattureGrigliaFiltriHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFattureHttpTestsConFlagSpenti : GestioneFattureHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFattureModificaHttpTestsConFlagSpenti : GestioneFattureModificaHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFattureNotaHttpTestsConFlagSpenti : GestioneFattureNotaHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFatturePosticipaBodyRealeHttpTestsConFlagSpenti : GestioneFatturePosticipaBodyRealeHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFatturePosticipaFatturaEmessaHttpTestsConFlagSpenti : GestioneFatturePosticipaFatturaEmessaHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFatturePosticipaFatturaFuturaHttpTestsConFlagSpenti : GestioneFatturePosticipaFatturaFuturaHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class GestioneFattureRipristinaHttpTestsConFlagSpenti : GestioneFattureRipristinaHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class JwtBearerPipelineHttpTestsConFlagSpenti : JwtBearerPipelineHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class LanguageServiceHttpTestsConFlagSpenti : LanguageServiceHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class ModuloCommessaDocumentoDownloadHttpTestsConFlagSpenti : ModuloCommessaDocumentoDownloadHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class ModuloCommessaHttpTestsConFlagSpenti : ModuloCommessaHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class NonceMultiTabsHttpTestsConFlagSpenti : NonceMultiTabsHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class NotificheRicercaHttpTestsConFlagSpenti : NotificheRicercaHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class OrchestratoreHttpTestsConFlagSpenti : OrchestratoreHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class PolicyAuthorizationHttpTestsConFlagSpenti : PolicyAuthorizationHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class ProdottoTokenAdversarialHttpTestsConFlagSpenti : ProdottoTokenAdversarialHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class PSPAdversarialHttpTestsConFlagSpenti : PSPAdversarialHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class PSPHttpTestsConFlagSpenti : PSPHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class RelDettaglioHttpTestsConFlagSpenti : RelDettaglioHttpTests { }

[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
[Category("FeatureFlags")]
public class RelDocumentoDownloadHttpTestsConFlagSpenti : RelDocumentoDownloadHttpTests { }
/// <summary>
/// Guardrail: ogni suite HTTP fuori dai flag ha la sua gemella "ConFlagSpenti" qui sopra, e nessuna
/// suite delle rotte sotto flag ce l'ha (fallirebbe con 404 per costruzione).
/// </summary>
[Category("FeatureFlags")]
public class FlagSpentiCoperturaTests
{
    private static readonly string[] SottoFlag = ["AppIo", "InvioSap", "FattureInvioSapPosticipate", "FeatureFlags"];

    private static readonly Type[] Suite = typeof(FlagSpentiCoperturaTests).Assembly.GetTypes()
        .Where(t => t.Namespace == typeof(FlagSpentiCoperturaTests).Namespace && t.IsClass && !t.IsAbstract
            && t.Name.EndsWith("HttpTests") && t.GetCustomAttributes(typeof(PropertyAttribute), false).Length == 0)
        .ToArray();

    private static readonly HashSet<Type> Gemelle = typeof(FlagSpentiCoperturaTests).Assembly.GetTypes()
        .Where(t => t.Name.EndsWith("ConFlagSpenti"))
        .Select(t => t.BaseType!)
        .ToHashSet();

    [Test]
    public void OgniSuiteFuoriDaiFlag_ShouldAvere_LaGemellaConFlagSpenti()
    {
        var mancanti = Suite.Where(t => !SottoFlag.Any(t.Name.StartsWith) && !Gemelle.Contains(t)).Select(t => t.Name);

        Assert.That(mancanti, Is.Empty,
            "aggiungere in FlagSpentiHttpTests.cs: [Property(ApiTestFactory.ProprietaFlagSpenti, \"true\")] public class <Suite>ConFlagSpenti : <Suite> { }");
    }

    [Test]
    public void NessunaSuiteSottoFlag_ShouldAvere_LaGemellaConFlagSpenti()
    {
        var sbagliate = Gemelle.Where(t => SottoFlag.Any(t.Name.StartsWith)).Select(t => t.Name);

        Assert.That(sbagliate, Is.Empty, "le rotte sotto flag con i flag spenti rispondono 404 per costruzione");
    }

    [Test]
    public void Gemelle_ShouldAvere_LaProprietaFlagSpenti()
    {
        var senza = typeof(FlagSpentiCoperturaTests).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("ConFlagSpenti"))
            .Where(t => !t.GetCustomAttributes(typeof(PropertyAttribute), false).Cast<PropertyAttribute>()
                .Any(p => p.Properties.ContainsKey(ApiTestFactory.ProprietaFlagSpenti)))
            .Select(t => t.Name);

        Assert.That(senza, Is.Empty, "senza la proprieta' la gemella rigirerebbe i test con i flag accesi");
    }
}

/// <summary>
/// La proprieta' spegne davvero i flag: con la proprieta' sulla fixture, una factory creata senza elenco
/// non registra nessuna rotta sotto flag; senza, le registra tutte.
/// </summary>
[Category("FeatureFlags")]
[Property(ApiTestFactory.ProprietaFlagSpenti, "true")]
public class ProprietaFlagSpentiTests
{
    internal static List<string> RotteSottoFlag(ApiTestFactory factory)
    {
        factory.CreateClient().Dispose();
        return factory.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>().Endpoints
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .Select(e => "/" + e.RoutePattern.RawText?.TrimStart('/'))
            .Where(r => r.StartsWith("/api/appio/") || r.StartsWith("/api/fatture/invio-sap")
                || r.StartsWith("/api/fatture/invio/") || r == "/api/fatture/resetta/pipeline")
            .ToList();
    }

    [Test]
    public void FactoryDefault_ConLaProprieta_ShouldSpegnere_TuttiIFlag()
    {
        using var factory = new ApiTestFactory();
        Assert.That(RotteSottoFlag(factory), Is.Empty);
    }

    [Test]
    public void ElencoEsplicito_ShouldVincere_SullaProprieta()
    {
        using var factory = new ApiTestFactory(FeatureFlags.Tutti);
        Assert.That(RotteSottoFlag(factory), Is.Not.Empty);
    }
}

[Category("FeatureFlags")]
public class SenzaProprietaFlagSpentiTests
{
    [Test]
    public void FactoryDefault_SenzaLaProprieta_ShouldAccendere_TuttiIFlag()
    {
        using var factory = new ApiTestFactory();
        Assert.That(ProprietaFlagSpentiTests.RotteSottoFlag(factory), Is.Not.Empty);
    }
}
