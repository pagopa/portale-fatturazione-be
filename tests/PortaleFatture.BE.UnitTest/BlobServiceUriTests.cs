using PortaleFatture_BE_SendEmailFunction;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Risoluzione dell'endpoint del servizio blob su cui `CreateRelRighe` carica i CSV di dettaglio REL.
///
/// Il ramo che conta qui e' il **default**, cioe' quello di produzione: `StorageRELBlobEndpoint` non
/// e' impostata su nessuna Function App, quindi l'URI deve restare quello pubblico dell'account,
/// identico a prima che l'override esistesse.
///
/// Nessun test end-to-end puo' coprirlo — in locale quella variabile c'e' sempre, altrimenti la
/// function non parlerebbe con Azurite. Senza questi casi, invertire la condizione del ternario
/// dirotterebbe ogni upload di produzione lasciando l'intera suite verde: e' esattamente il tipo di
/// difetto che non si manifesta finche' non e' in produzione.
/// </summary>
public class BlobServiceUriTests
{
    private const string Account = "fatpsa";

    /// <summary>Il comportamento di sempre, quello che gira sulle Function App.</summary>
    [Test]
    public void SenzaOverride_ShouldUsareEndpointPubblicoDellAccount()
    {
        var uri = CreateRelRighe.ResolveBlobServiceUri(Account, null);

        Assert.Multiple(() =>
        {
            Assert.That(uri.ToString(), Is.EqualTo($"https://{Account}.blob.core.windows.net/"));
            Assert.That(uri.Scheme, Is.EqualTo("https"), "mai in chiaro verso un account reale");
            Assert.That(uri.Host, Is.EqualTo($"{Account}.blob.core.windows.net"),
                "forma host-style: il nome dell'account sta nell'host");
        });
    }

    /// <summary>
    /// Una variabile presente ma vuota o di soli spazi e' un errore di configurazione, non una scelta:
    /// deve comportarsi come se non ci fosse. E' il caso realistico di un app setting creato e mai
    /// valorizzato, che senza il controllo su `IsNullOrWhiteSpace` farebbe fallire `new Uri("")` con
    /// una `UriFormatException` all'avvio del primo upload.
    /// </summary>
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t")]
    public void OverrideVuotoOSoliSpazi_ShouldComportarsiComeAssente(string override_)
    {
        var uri = CreateRelRighe.ResolveBlobServiceUri(Account, override_);

        Assert.That(uri.ToString(), Is.EqualTo($"https://{Account}.blob.core.windows.net/"));
    }

    /// <summary>
    /// Con l'override si usa quello e basta: il nome dell'account NON viene piu' usato per comporre
    /// l'host. E' cio' che permette la forma path-style dell'emulatore, dove l'account sta nel
    /// percorso.
    /// </summary>
    [Test]
    public void ConOverride_ShouldUsarloTaleEQuale_AncheInFormaPathStyle()
    {
        var uri = CreateRelRighe.ResolveBlobServiceUri(Account, "http://azurite:10000/devstoreaccount1");

        Assert.Multiple(() =>
        {
            Assert.That(uri.ToString(), Is.EqualTo("http://azurite:10000/devstoreaccount1"));
            Assert.That(uri.Host, Is.EqualTo("azurite"));
            Assert.That(uri.AbsolutePath, Is.EqualTo("/devstoreaccount1"),
                "forma path-style: l'account sta nel percorso, non nell'host");
            Assert.That(uri.ToString(), Does.Not.Contain(Account),
                "con l'override il nome dell'account non entra piu' nell'endpoint");
        });
    }

    /// <summary>
    /// CARATTERIZZAZIONE. Un override sintatticamente non valido solleva `UriFormatException` invece
    /// di ricadere sul default. E' il comportamento preferibile — un endpoint scritto male e' un
    /// errore di configurazione da far emergere, non da mascherare dirottando su quello pubblico —
    /// ma va saputo che l'eccezione arriva dentro `AddDocument`, quindi si presenta come un
    /// fallimento dell'upload e non della configurazione.
    /// </summary>
    [Test]
    public void OverrideMalformato_ShouldSollevare_NonRicadereSulDefault_Caratterizzazione()
    {
        Assert.Throws<UriFormatException>(() =>
            CreateRelRighe.ResolveBlobServiceUri(Account, "non-e-un-url"));
    }
}
