using System.Net;
using PortaleFatture.BE.Function.API.Extensions;

namespace PortaleFatture.BE.UnitTest.FunctionApi;

/// <summary>
/// Input ostili su ApiExtensions. Non e' un esercizio di stile: questi due metodi — SkipSwagger e
/// ExtractIpAddress — sono l'unica autenticazione della Integration API, perche' gli HttpTrigger
/// sono AuthorizationLevel.Anonymous e il controllo nativo a chiave di Azure Functions non e' usato
/// (v. docs/integration-api-riferimento.md). Chi chiama controlla URL e header: sono esattamente le
/// due cose da cui questi metodi ricavano "posso saltare l'autenticazione" e "da che IP arrivi".
///
/// Diversi test qui sono CARATTERIZZAZIONI: fissano il comportamento reale, difetti compresi, non
/// quello desiderabile. Dove il comportamento e' sbagliato il test lo dice nel nome e nel commento.
/// </summary>
[TestFixture]
public class ApiExtensionsAdversarialTests
{
    private static FakeHttpRequestData Richiesta(string url) => new(new FakeFunctionContext(), url);

    // --- SkipSwagger: il bypass ---------------------------------------------------------------

    /// <summary>
    /// 🔴 SkipSwagger cerca "/api/swagger" come SOTTOSTRINGA dell'URL INTERO, query string compresa
    /// (Url.ToString()), invece di guardare il solo path. Una rotta di business con quella stringa
    /// in un parametro di query viene quindi trattata come una richiesta a Swagger, e il middleware
    /// fa `next(context)` SENZA verificare ne' la chiave API ne' la whitelist degli IP.
    ///
    /// Non serve conoscere una chiave valida: basta la stringa nell'URL. E siccome il controllo
    /// avviene prima di ogni altra cosa, vale per tutte le rotte v1/*.
    ///
    /// Caratterizzazione del comportamento reale: il giorno in cui SkipSwagger guardera' il path,
    /// questo test diventa rosso ed e' il segnale che il difetto e' stato chiuso (va allora
    /// riscritto come Is.False, non cancellato).
    /// </summary>
    [TestCase("https://host/api/v1/notifiche/periodo?x=/api/swagger")]
    [TestCase("https://host/api/v1/fatture/ricerca?redirect=/api/swagger.json")]
    [TestCase("https://host/api/v1/rel/periodo?a=1&b=/api/swagger&c=2")]
    public void SkipSwagger_ConLaStringaNellaQueryString_SaltaLAutenticazione_Caratterizzazione(string url)
    {
        Assert.That(Richiesta(url).SkipSwagger(), Is.True,
            "Se questo e' diventato False il difetto e' stato corretto: riscrivere l'asserzione.");
    }

    /// <summary>
    /// Stessa causa, variante diversa: la stringa puo' stare anche in un segmento di path qualsiasi,
    /// non solo in query. Serve a mostrare che non e' un caso particolare della query string ma
    /// della ricerca per sottostringa.
    /// </summary>
    [Test]
    public void SkipSwagger_ConLaStringaInUnSegmentoDiPath_SaltaLAutenticazione_Caratterizzazione()
    {
        Assert.That(Richiesta("https://host/api/v1/contestazioni/api/swagger/upload").SkipSwagger(), Is.True);
    }

    /// <summary>
    /// La seconda condizione di SkipSwagger e' codice morto: "/api/swagger.json" contiene gia'
    /// "/api/swagger", quindi il primo Contains e' sempre vero prima che il secondo venga valutato.
    /// Documentato perche' chi legge il metodo pensa che siano due controlli distinti.
    /// </summary>
    [Test]
    public void SkipSwagger_LaSecondaCondizione_ERidondante()
    {
        const string url = "https://host/api/swagger.json";

        Assert.That(url.Contains("/api/swagger", StringComparison.OrdinalIgnoreCase), Is.True,
            "Il primo Contains copre gia' il secondo: la condizione su '/api/swagger.json' non e' mai decisiva.");
    }

    // --- ExtractIpAddress: la whitelist si fida del chiamante ----------------------------------

    /// <summary>
    /// ⚠️ L'indirizzo su cui si basa la whitelist arriva da HEADER FORNITI DAL CLIENT. Un chiamante
    /// che conosca un IP autorizzato puo' dichiararlo e passare il controllo, a meno che il gateway
    /// davanti alla function non sovrascriva quegli header.
    ///
    /// Il caso peggiore e' il primo della catena: `custom-forwarded-for` non e' un header standard,
    /// quindi un proxy che ripulisce diligentemente X-Forwarded-For puo' lasciarlo passare intatto —
    /// e avendo la precedenza vince anche su un X-Forwarded-For scritto correttamente dal gateway.
    ///
    /// Il test NON afferma che il sistema sia bucato: afferma che la robustezza della whitelist
    /// dipende interamente dall'igiene degli header a monte, che e' fuori da questo codice. Da
    /// verificare sull'Application Gateway prima di considerare la whitelist una barriera.
    /// </summary>
    [Test]
    public void ExtractIpAddress_CustomForwardedForDelClient_SovrascriveXForwardedFor()
    {
        var req = Richiesta("https://host/api/v1/notifiche/periodo")
            .ConHeader("x-forwarded-for", "198.51.100.99")       // poniamo: scritto dal gateway
            .ConHeader("custom-forwarded-for", "203.0.113.10");  // dichiarato dal client

        Assert.That(req.ExtractIpAddress(), Is.EqualTo("203.0.113.10"),
            "Vince l'header non standard, quindi anche un X-Forwarded-For attendibile viene scavalcato.");
    }

    /// <summary>
    /// 🔴 Gli indirizzi IPv6 vengono TRONCATI. Lo spezzone `Split(':')[0]` serve a togliere la porta
    /// dalla forma "1.2.3.4:5678", ma un IPv6 nudo e' pieno di ':' e non ha parentesi quadre: di
    /// "2001:db8::1" resta "2001".
    ///
    /// Conseguenze, entrambe reali: un chiamante IPv6 non puo' mai combaciare con la whitelist
    /// (403 inspiegabile per lui), e il residuo non e' un indirizzo valido — v. il test successivo
    /// per cosa succede nel middleware.
    /// </summary>
    [TestCase("2001:db8::1", "2001")]
    [TestCase("fe80::1ff:fe23:4567:890a", "fe80")]
    [TestCase("::1", "")]
    public void ExtractIpAddress_ConIPv6Nudo_LoTronca_Caratterizzazione(string header, string atteso)
    {
        var req = Richiesta("https://host/api/v1/notifiche/periodo").ConHeader("x-forwarded-for", header);

        var risultato = req.ExtractIpAddress();

#if DEBUG
        // In Debug il ramo #if DEBUG di ExtractIpAddress rimappa "::1" e la stringa vuota su
        // 127.0.0.1: per quel caso il troncamento non e' osservabile. V. il test dedicato.
        if (atteso.Length == 0)
        {
            Assert.That(risultato, Is.EqualTo("127.0.0.1"));
            return;
        }
#endif
        Assert.That(risultato, Is.EqualTo(atteso));
    }

    /// <summary>
    /// La forma con parentesi quadre e porta, che e' quella che Azure produce per un client IPv6,
    /// viene invece gestita correttamente. Serve a delimitare il difetto precedente: non e' "IPv6
    /// non funziona", e' "IPv6 senza parentesi quadre non funziona".
    /// </summary>
    [Test]
    public void ExtractIpAddress_ConIPv6TraParentesiEPorta_ShouldEstrarreLIndirizzo()
    {
        var req = Richiesta("https://host/api/v1/notifiche/periodo")
            .ConHeader("x-forwarded-for", "[2001:db8::1]:51234");

        Assert.That(req.ExtractIpAddress(), Is.EqualTo("2001:db8::1"));
    }

    /// <summary>
    /// 🔴 Il seguito del troncamento e' peggio di un errore: il residuo NON fa lanciare
    /// IPAddress.Parse, viene interpretato con il parsing legacy come intero a 32 bit e diventa un
    /// indirizzo IPv4 valido ma completamente diverso. Misurato: "2001:db8::1" -> "2001" ->
    /// 0.0.7.209.
    ///
    /// E' il ramo CIDR di AuthMiddleware (`IPNetwork.Contains(IPAddress.Parse(ipAddress))`): il
    /// confronto con la whitelist avviene quindi su un indirizzo inventato, senza eccezione e senza
    /// nulla nei log che lo segnali. Un'eccezione sarebbe stata preferibile: almeno si vedeva.
    ///
    /// Corollario: siccome qualunque intero decimale e' un IP valido per Parse, un header come
    /// "3232235776:x" diventa 192.168.1.0. Non da' poteri nuovi a un attaccante — potrebbe scrivere
    /// l'IP direttamente — ma rende la whitelist confrontabile con valori che non sembrano IP.
    /// </summary>
    [Test]
    public void IPAddressParse_SulResiduoDiUnIPv6Troncato_ProduceUnIndirizzoDiverso_Caratterizzazione()
    {
        var req = Richiesta("https://host/api/v1/notifiche/periodo")
            .ConHeader("x-forwarded-for", "2001:db8::1");

        var estratto = req.ExtractIpAddress();

        Assert.That(estratto, Is.EqualTo("2001"));
        Assert.Multiple(() =>
        {
            Assert.That(IPAddress.Parse(estratto!).ToString(), Is.EqualTo("0.0.7.209"),
                "Parsing legacy dell'intero: nessuna eccezione, indirizzo valido e sbagliato.");
            Assert.That(IPAddress.Parse("3232235776").ToString(), Is.EqualTo("192.168.1.0"),
                "Stessa regola: un intero decimale qualunque e' accettato come IPv4.");
        });
    }

    /// <summary>
    /// 🔴 Un header presente ma VUOTO non fa proseguire la catena di fallback: "" non e' null, quindi
    /// il `??` si ferma li' e gli altri due header non vengono nemmeno guardati. Con un ente che ha
    /// un CIDR in whitelist si finisce di nuovo su IPAddress.Parse("") — 500, non 403.
    ///
    /// E' l'input piu' banale da produrre: basta mandare l'header senza valore.
    /// </summary>
    [Test]
    public void ExtractIpAddress_ConHeaderVuoto_InterrompeIlFallback_Caratterizzazione()
    {
        var req = Richiesta("https://host/api/v1/notifiche/periodo")
            .ConHeader("x-forwarded-for", "")
            .ConHeader("x-original-for", "203.0.113.30");

        var risultato = req.ExtractIpAddress();

#if DEBUG
        Assert.That(risultato, Is.EqualTo("127.0.0.1"),
            "In Debug il ramo #if DEBUG maschera il difetto rimappando la stringa vuota.");
#else
        Assert.That(risultato, Is.Empty,
            "L'header valido x-original-for viene ignorato perche' il fallback si e' gia' fermato.");
        Assert.Throws<FormatException>(() => IPAddress.Parse(risultato!));
#endif
    }

    /// <summary>
    /// ⚠️ Trappola di testing, non difetto di prodotto: ExtractIpAddress ha un ramo `#if DEBUG` che
    /// rimappa IP assente/localhost su 127.0.0.1. `dotnet test` compila in Debug, quindi TUTTI i
    /// test di questo metodo vedono un comportamento che in produzione non esiste.
    ///
    /// Va saputo prima di fidarsi di un verde: il caso "nessun header" in produzione restituisce
    /// null, qui restituisce 127.0.0.1. Per esercitare davvero il ramo di produzione servirebbe
    /// `dotnet test -c Release`.
    /// </summary>
    [Test]
    public void ExtractIpAddress_SenzaAlcunHeader_DipendeDallaConfigurazioneDiBuild()
    {
        var risultato = Richiesta("https://host/api/v1/notifiche/periodo").ExtractIpAddress();

#if DEBUG
        Assert.That(risultato, Is.EqualTo("127.0.0.1"),
            "Ramo #if DEBUG: in produzione (Release) qui ci sarebbe null.");
#else
        Assert.That(risultato, Is.Null);
#endif
    }

    /// <summary>
    /// Uno spazio dopo la virgola e' legittimo in X-Forwarded-For e viene gestito (Trim). Serve come
    /// contro-prova: non tutto quello che si prova qui e' rotto.
    /// </summary>
    [Test]
    public void ExtractIpAddress_ConSpaziNellaLista_ShouldRipulirli()
    {
        var req = Richiesta("https://host/api/v1/notifiche/periodo")
            .ConHeader("x-forwarded-for", "   203.0.113.10   ,   198.51.100.7");

        Assert.That(req.ExtractIpAddress(), Is.EqualTo("203.0.113.10"));
    }

    // --- GetUri: il dominio pubblico e' una configurazione, non un dato validato ---------------

    /// <summary>
    /// ⚠️ CustomDomain non configurato fa esplodere GetUri con una NullReferenceException, dentro la
    /// composizione della risposta 202 di OGNI handler (statusQueryGetUri). Non e' un errore di
    /// input del chiamante: e' un ambiente configurato a meta' che si manifesta come 500 su tutte
    /// le rotte, e il messaggio non dice quale impostazione manca.
    /// </summary>
    [Test]
    public void GetUri_ConCustomDomainNonConfigurato_Lancia_Caratterizzazione()
    {
        var configurazione = new FakeConfigurazione { CustomDomain = null };

        Assert.Throws<NullReferenceException>(
            () => configurazione.GetUri("http://localhost:7071/runtime/webhooks/durabletask/instances/abc"));
    }

    /// <summary>
    /// ⚠️ Il dominio configurato finisce come stringa di SOSTITUZIONE di Regex.Replace, dove "$" ha
    /// significato speciale ($1, $&amp;, $$...). Un dominio che contenga un "$" non viene copiato
    /// alla lettera. E' improbabile in un nome DNS, ma il punto e' un altro: un valore di
    /// configurazione viene interpretato come pattern invece che come testo. Il rimedio e'
    /// Regex.Escape sul replacement, o una sostituzione che non passi dalle regex.
    /// </summary>
    [Test]
    public void GetUri_ConDollaroNelDominio_LoInterpretaComeGruppoDiCattura_Caratterizzazione()
    {
        var configurazione = new FakeConfigurazione { CustomDomain = "https://a$&b.example/api" };

        var risultato = configurazione.GetUri("http://localhost:7071/x");

        Assert.That(risultato, Does.Not.Contain("$&"),
            "$& viene espanso nel testo che la regex ha catturato, invece di restare letterale.");
        Assert.That(risultato, Does.Contain("http://localhost:7071"),
            "Cioe' l'host interno che si voleva nascondere ricompare nell'URL restituito.");
    }

    /// <summary>
    /// Un URL che non inizia per http/https non corrisponde al pattern, quindi resta intatto: non
    /// lancia, ma restituisce un URL interno non riscritto. Caratterizzato perche' il metodo sembra
    /// garantire la riscrittura e in questo caso non la fa.
    /// </summary>
    [Test]
    public void GetUri_ConUriSenzaSchema_LoLasciaIntatto_Caratterizzazione()
    {
        var configurazione = new FakeConfigurazione { CustomDomain = "https://pubblico.example/api" };

        Assert.That(configurazione.GetUri("localhost:7071/runtime/x"), Is.EqualTo("localhost:7071/runtime/x"));
    }
}
