using System.Text;
using System.Text.Json;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>Chiamate HTTP dell'Invio a SAP come admin pagoPA, con il nonce gia' agganciato.</summary>
internal static class InvioSapHttp
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    public static async Task<HttpResponseMessage> Invia(ApiTestFactory factory, HttpMethod metodo, string rotta, string? body, string? ruolo = Ruolo.ADMIN)
    {
        var client = factory.CreateClientAs(ruolo);
        using var req = new HttpRequestMessage(metodo, factory.WithNonce(rotta));
        if (body is not null) req.Content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await client.SendAsync(req);
        TestContext.Out.WriteLine($"{metodo} {rotta} -> {(int)resp.StatusCode}");
        return resp;
    }

    public static Task<HttpResponseMessage> Post(ApiTestFactory f, string rotta, string body) => Invia(f, HttpMethod.Post, rotta, body);
    public static Task<HttpResponseMessage> Put(ApiTestFactory f, string rotta, string body) => Invia(f, HttpMethod.Put, rotta, body);
    public static Task<HttpResponseMessage> Get(ApiTestFactory f, string rotta) => Invia(f, HttpMethod.Get, rotta, null);

    public static async Task<JsonElement> Json(HttpResponseMessage resp)
    {
        var testo = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(testo);
        return doc.RootElement.Clone();
    }

    /// <summary>Body del Genera serializzato con i nomi del contratto (camelCase).</summary>
    public static string BodyGenera(int anno, int mese, string tipologia, string? nota, long[]? idFatture = null, string? storno = null)
        => JsonSerializer.Serialize(new { anno, mese, tipologiaFattura = tipologia, nota, idFatture, numeroDocumentoStorno = storno }, Web);
}
