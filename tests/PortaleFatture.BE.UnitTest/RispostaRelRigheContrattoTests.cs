using System.Text.Json;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture_BE_SendEmailFunction.Models;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Il **contratto della risposta** di `CreateRelRighe`, cioe' la stringa che il team DATA legge
/// facendo polling sullo stato dell'orchestrazione:
///
///     GET {host}/runtime/webhooks/durabletask/instances/{instanceId}?code={systemKey}
///     -> { "runtimeStatus": "Completed", ..., "output": "&lt;questa stringa JSON&gt;" }
///
/// Fino al 21/09/2026 il ramo "nessuna REL per il periodo" sollevava una `DomainException`, quindi
/// il chiamante vedeva `runtimeStatus = Failed` e uno stack trace. Da PF-882 quel ramo **restituisce**
/// la risposta, l'orchestrazione chiude in `Completed` e l'esito si legge nei campi qui sotto: e'
/// questa la differenza che la pipeline deve poter interpretare.
///
/// Unit e non integration di proposito: qui non si verifica *quando* la risposta viene prodotta (lo
/// fa `CreateRelRigheAttivitaIntegrationTests` sul DB seedato) ma la **forma** del JSON, che e' cio'
/// che rompe un consumatore esterno se qualcuno rinomina una proprieta' o cambia il serializzatore.
/// Nessun test lo presidiava, e il compilatore non puo' accorgersene.
/// </summary>
public class RispostaRelRigheContrattoTests
{
    private static readonly string[] ProprietaAttese =
        ["Anno", "Mese", "TipologiaFattura", "Count", "DbConnection", "Error"];

    /// <summary>
    /// I nomi sono in **PascalCase**: `SerializationExtensions._options` imposta la camelCase solo
    /// sulle chiavi dei dizionari (`DictionaryKeyPolicy`), non sui nomi delle proprieta'. Un
    /// `PropertyNamingPolicy = CamelCase` aggiunto un domani a quelle opzioni cambierebbe il JSON di
    /// TUTTE le function in un colpo solo, senza che nulla fallisca a compile-time.
    /// </summary>
    [Test]
    public void Serialize_ShouldEsporreEsattamenteLeSeiProprietaDelContratto()
    {
        var json = new RispostaRelRighe { Anno = 2026, Mese = 6, TipologiaFattura = "SECONDO SALDO" }.Serialize();

        using var doc = JsonDocument.Parse(json);
        var nomi = doc.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

        Assert.That(nomi, Is.EquivalentTo(ProprietaAttese),
            "il polling della pipeline DATA legge questi nomi: rinominarne uno e' un cambio di contratto");
    }

    /// <summary>
    /// Il payload del ramo corretto da PF-882. Le tre asserzioni sono quelle su cui la pipeline puo'
    /// distinguere "periodo vuoto" da "andato a buon fine": `Count` **numerico 0**, `DbConnection`
    /// ancora `true` (il DB ha risposto) e `Error` valorizzato con il motivo.
    /// </summary>
    [Test]
    public void Serialize_NessunaRel_ShouldAvereCountZeroNumericoEDbConnectionTrue()
    {
        var risposta = new RispostaRelRighe
        {
            Anno = 2026,
            Mese = 6,
            TipologiaFattura = "SECONDO SALDO",
            Count = 0,
            Error = "Non ci sono rel per l'anno, mese e tipologia specificate"
        };

        using var doc = JsonDocument.Parse(risposta.Serialize());
        var root = doc.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("Count").ValueKind, Is.EqualTo(JsonValueKind.Number),
                "0 e' un numero, non null: e' il discriminante fra 'nessuna REL' e 'non ci sono arrivato'");
            Assert.That(root.GetProperty("Count").GetInt32(), Is.Zero);
            Assert.That(root.GetProperty("DbConnection").GetBoolean(), Is.True,
                "il DB ha risposto: l'assenza di REL non e' un guasto di connessione");
            Assert.That(root.GetProperty("Error").GetString(), Is.Not.Null.And.Not.Empty);
            Assert.That(root.GetProperty("Anno").ValueKind, Is.EqualTo(JsonValueKind.Number),
                "Anno e Mese viaggiano come numeri: sono gia' stati convertiti da Convert.ToInt32");
            Assert.That(root.GetProperty("Mese").ValueKind, Is.EqualTo(JsonValueKind.Number));
        });
    }

    /// <summary>
    /// `Count` e' `int?`: resta **null** se l'esecuzione non e' arrivata alla query. Vale la pena
    /// fissarlo perche' e' l'altra meta' del discriminante sopra — null non significa "zero righe".
    /// </summary>
    [Test]
    public void Serialize_CountNonValorizzato_ShouldRestareNull()
    {
        var json = new RispostaRelRighe { Anno = 2026, Mese = 6 }.Serialize();

        using var doc = JsonDocument.Parse(json);

        Assert.That(doc.RootElement.GetProperty("Count").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    /// <summary>Default della classe: `DbConnection` parte a true, `Count` e `Error` a null.</summary>
    [Test]
    public void NuovaRisposta_ShouldPartireConDbConnectionTrueECountNullo()
    {
        var risposta = new RispostaRelRighe();

        Assert.Multiple(() =>
        {
            Assert.That(risposta.DbConnection, Is.True);
            Assert.That(risposta.Count, Is.Null);
            Assert.That(risposta.Error, Is.Null);
        });
    }

    /// <summary>
    /// CARATTERIZZAZIONE, non requisito. L'apostrofo di "l'anno" esce come `\u0027` perche' le opzioni
    /// di serializzazione non impostano un `Encoder` e vale quindi il `JavaScriptEncoder.Default`,
    /// che escapa i caratteri potenzialmente pericolosi in HTML.
    ///
    /// E' documentato qui perche' quel `\u0027`, letto in un messaggio d'errore accanto alla parola
    /// "anno", e' gia' stato scambiato per un problema di decodifica del parametro `Anno` (che non
    /// c'entra nulla: v. `docs/pipeline-dati-send.md`). Se un domani si passa a
    /// `UnsafeRelaxedJsonEscaping` questo test diventa rosso ed e' corretto aggiornarlo — ma va
    /// saputo che quella modifica cambia l'output di ogni serializzazione del progetto.
    /// </summary>
    [Test]
    public void Serialize_MessaggioConApostrofo_ShouldEscaparloInUnicode_Caratterizzazione()
    {
        var json = new RispostaRelRighe { Error = "Non ci sono rel per l'anno" }.Serialize();

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain(@"l\u0027anno"), "e' l'apostrofo, non un problema di encoding");
            using var doc = JsonDocument.Parse(json);
            Assert.That(doc.RootElement.GetProperty("Error").GetString(), Does.Contain("l'anno"),
                "un parser JSON qualsiasi lo rilegge correttamente: l'escape e' solo nel testo grezzo");
        });
    }
}
