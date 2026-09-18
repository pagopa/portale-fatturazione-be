using System.Collections;
using System.Collections.Immutable;

using System.Net;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.UnitTest.FunctionApi;

/// <summary>
/// Fake minimi dei tipi astratti del worker isolato di Azure Functions, condivisi dai test
/// dell'area Function.API.
///
/// Perche' scritti a mano e non con Moq: HttpRequestData e FunctionContext sono classi astratte con
/// un costruttore che pretende gia' un FunctionContext, e meta' delle proprieta' non servono a
/// nessuno di questi test. Un mock andrebbe configurato proprieta' per proprieta' in ogni fixture,
/// e un membro non configurato restituirebbe null senza dirlo — cioe' il test fallirebbe per il
/// motivo sbagliato. Qui invece cio' che non e' implementato lancia, in modo esplicito.
/// </summary>
internal sealed class FakeFunctionContext : FunctionContext
{
    public override string InvocationId { get; } = Guid.NewGuid().ToString();
    public override string FunctionId { get; } = "test-function-id";
    public override TraceContext TraceContext => throw new NotSupportedException("Non usato dai test.");
    public override BindingContext BindingContext => throw new NotSupportedException("Non usato dai test.");
    public override RetryContext RetryContext => throw new NotSupportedException("Non usato dai test.");
    public override IServiceProvider InstanceServices { get; set; } = null!;
    public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

    // Impostabili dai test che montano il middleware: AuthMiddleware legge FunctionDefinition per
    // capire se l'invocazione e' un httpTrigger, e Features per recuperare la richiesta HTTP.
    public FunctionDefinition? DefinizioneImpostata { get; set; }
    public IInvocationFeatures? FeatureImpostate { get; set; }

    public override FunctionDefinition FunctionDefinition =>
        DefinizioneImpostata ?? throw new NotSupportedException("FunctionDefinition non impostata dal test.");

    public override IInvocationFeatures Features =>
        FeatureImpostate ?? throw new NotSupportedException("Features non impostate dal test.");
}

/// <summary>
/// FunctionDefinition con un solo binding di input: e' cio' che AuthMiddleware guarda per decidere
/// se l'invocazione e' HTTP (`InputBindings["req"].Type == "httpTrigger"`) oppure, ad esempio, una
/// activity di orchestrazione — nel qual caso lascia passare senza autenticare.
/// </summary>
internal sealed class FakeFunctionDefinition(string nomeBinding, string tipoBinding) : FunctionDefinition
{
    public override string PathToAssembly { get; } = "test.dll";
    public override string EntryPoint { get; } = "Test.Entry";
    public override string Id { get; } = "test-id";
    public override string Name { get; } = "TestFunction";
    public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; } =
        ImmutableDictionary<string, BindingMetadata>.Empty.Add(nomeBinding, new FakeBindingMetadata(nomeBinding, tipoBinding));
    public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; } =
        ImmutableDictionary<string, BindingMetadata>.Empty;
    public override ImmutableArray<FunctionParameter> Parameters { get; } = [];
}

internal sealed class FakeBindingMetadata(string name, string type) : BindingMetadata
{
    public override string Name { get; } = name;
    public override string Type { get; } = type;
    public override BindingDirection Direction { get; } = BindingDirection.In;
}

/// <summary>
/// Contenitore delle feature dell'invocazione, indicizzate per tipo. Serve a consegnare al
/// middleware la IHttpRequestDataFeature qui sotto.
/// </summary>
internal sealed class FakeInvocationFeatures : IInvocationFeatures
{
    private readonly Dictionary<Type, object> _feature = [];

    public T? Get<T>() => _feature.TryGetValue(typeof(T), out var valore) ? (T)valore : default;
    public void Set<T>(T instance) => _feature[typeof(T)] = instance!;
    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => _feature.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _feature.GetEnumerator();
}

/// <summary>
/// La via per consegnare al middleware una richiesta HTTP finta.
///
/// GetHttpRequestDataAsync cerca prima IHttpRequestDataFeature fra le feature e solo in sua assenza
/// ripiega sull'implementazione predefinita, che passa per IFunctionBindingsFeature — interfaccia
/// **internal** dell'SDK, quindi non implementabile da qui. Fornendo questa, quel ramo non viene
/// mai raggiunto.
/// </summary>
internal sealed class FakeHttpRequestDataFeature(HttpRequestData richiesta) : IHttpRequestDataFeature
{
    public ValueTask<HttpRequestData?> GetHttpRequestDataAsync(FunctionContext context) => new(richiesta);
}

internal sealed class FakeHttpRequestData : HttpRequestData
{
    public FakeHttpRequestData(FunctionContext context, string url, string method = "GET", Stream? body = null)
        : base(context)
    {
        Url = new Uri(url);
        Method = method;
        Body = body ?? new MemoryStream();
    }

    public override Stream Body { get; }
    public override HttpHeadersCollection Headers { get; } = new();
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = [];
    public override Uri Url { get; }
    public override IEnumerable<ClaimsIdentity> Identities { get; } = [];
    public override string Method { get; }

    public FakeHttpRequestData ConHeader(string nome, string valore)
    {
        Headers.Add(nome, valore);
        return this;
    }

    /// <summary>
    /// AuthMiddleware crea la risposta di rifiuto e ritorna, senza passarla a nessuno che i test
    /// possano interrogare: registrandole qui si puo' asserire sullo status code prodotto.
    /// </summary>
    public List<FakeHttpResponseData> RisposteCreate { get; } = [];

    public override HttpResponseData CreateResponse()
    {
        var risposta = new FakeHttpResponseData(FunctionContext);
        RisposteCreate.Add(risposta);
        return risposta;
    }
}

internal sealed class FakeHttpResponseData(FunctionContext context) : HttpResponseData(context)
{
    public override HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public override HttpHeadersCollection Headers { get; set; } = new();
    public override Stream Body { get; set; } = new MemoryStream();
    public override HttpCookies Cookies => throw new NotSupportedException("Non usato dai test.");
}

internal sealed class FakeConfigurazione : IConfigurazione
{
    public string? AESKey { get; set; }
    public string? ConnectionString { get; set; }
    public string? CustomDomain { get; set; }
    public StorageNotifiche? StorageNotifiche { get; set; }
}
