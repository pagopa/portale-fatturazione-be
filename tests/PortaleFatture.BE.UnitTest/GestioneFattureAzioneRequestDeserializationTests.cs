using System.Text.Json;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Unit test PURI (nessun DB) sul contratto in ingresso dell'endpoint azione: verificano che
/// GestioneFattureAzioneRequest deserializzi correttamente il body JSON. In particolare blindano la
/// correzione 2026-07-28 IdFattura int -> long: un idFattura oltre int.MaxValue deve arrivare integro.
/// Se qualcuno riportasse la proprieta' a int?, questi test diventerebbero rossi.
/// Le opzioni sono quelle di ASP.NET Core (JsonSerializerDefaults.Web).
/// </summary>
public class GestioneFattureAzioneRequestDeserializationTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Test]
    public void Deserializza_IdFatturaOltreInt32_SenzaTroncare()
    {
        const long grande = 7770001234567L; // > int.MaxValue (2.147.483.647)
        var json = $$"""
        { "mese": 8, "anno": 2026, "tipologiaFattura": "SECONDO SALDO",
          "azione": "POSTICIPA", "idFattura": {{grande}}, "idEnte": "11111111-1111-1111-1111-111111111111" }
        """;

        var req = JsonSerializer.Deserialize<GestioneFattureAzioneRequest>(json, Web);

        Assert.That(req, Is.Not.Null);
        Assert.That(req!.IdFattura, Is.EqualTo(grande),
            "idFattura oltre int.MaxValue deve deserializzare integro (long?). Con int? qui ci sarebbe overflow.");
    }

    [Test]
    public void Deserializza_IdFatturaComeStringa_Accettato()
    {
        // ASP.NET usa NumberHandling.AllowReadingFromString: alcuni client mandano i numeri come stringa.
        var json = """{ "azione": "CANCELLA", "idFattura": "62899" }""";
        var req = JsonSerializer.Deserialize<GestioneFattureAzioneRequest>(json, Web);
        Assert.That(req!.IdFattura, Is.EqualTo(62899L));
    }

    [Test]
    public void Deserializza_IdFatturaNull_RestaNull()
    {
        // La posticipa/eliminazione PER PERIODO manda idFattura null (chiave = Anno/Mese/Ente/Tipologia).
        var json = """{ "azione": "POSTICIPA", "idFattura": null, "anno": 2026, "mese": 8 }""";
        var req = JsonSerializer.Deserialize<GestioneFattureAzioneRequest>(json, Web);
        Assert.That(req!.IdFattura, Is.Null);
    }
}
