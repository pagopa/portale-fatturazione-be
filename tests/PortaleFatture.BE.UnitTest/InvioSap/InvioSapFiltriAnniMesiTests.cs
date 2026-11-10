using System.Text.Json;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Exceptions;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// Filtri Anno e Mese della pagina Invio a SAP (<c>GET invio-sap/anni</c>, <c>POST invio-sap/mesi</c>, 11/11/2026):
/// i valori sono quelli presenti in griglia, quindi le due query leggono la stessa vista della griglia. Qui la
/// parte senza DB: body di <c>mesi</c> e regola sull'anno. L'SQL (builder internal) e' verificato sul DB seedato
/// da InvioSapLetturaHttpTests, confrontando le risposte con la vista.
/// </summary>
[Category("InvioSap")]
public class InvioSapFiltriAnniMesiTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    // ---- Body di mesi -------------------------------------------------------------------------

    [TestCase("""{ "anno": 2026 }""", 2026, TestName = "Mesi_Body_ShouldDeserializzare(anno intero)")]
    [TestCase("""{ "ANNO": 2026 }""", 2026, TestName = "Mesi_Body_ShouldDeserializzare(nome non case sensitive)")]
    [TestCase("""{ "anno": 2026, "mesi": [1], "altro": true }""", 2026, TestName = "Mesi_Body_ShouldDeserializzare(campi sconosciuti ignorati)")]
    [TestCase("""{ "anno": "2026" }""", 2026, TestName = "Mesi_Body_ShouldDeserializzare(anno numerico tra virgolette: tollerato dalle opzioni Web)")]
    [TestCase("{}", null, TestName = "Mesi_Body_ShouldDeserializzare(anno assente)")]
    [TestCase("""{ "anno": null }""", null, TestName = "Mesi_Body_ShouldDeserializzare(anno null)")]
    public void Mesi_Body_ShouldDeserializzare(string json, int? atteso)
        => Assert.That(JsonSerializer.Deserialize<InvioSapMesiRequest>(json, Web)!.Anno, Is.EqualTo(atteso));

    [TestCase("""{ "anno": "duemilaventisei" }""", TestName = "Mesi_BodyConTipoSbagliato_ShouldLanciareJsonException(anno testo)")]
    [TestCase("""{ "anno": "2026 OR 1=1" }""", TestName = "Mesi_BodyConTipoSbagliato_ShouldLanciareJsonException(injection nell'anno)")]
    [TestCase("""{ "anno": 2026.5 }""", TestName = "Mesi_BodyConTipoSbagliato_ShouldLanciareJsonException(anno decimale)")]
    [TestCase("""{ "anno": [2026] }""", TestName = "Mesi_BodyConTipoSbagliato_ShouldLanciareJsonException(anno array)")]
    [TestCase("""{ "anno": 99999999999 }""", TestName = "Mesi_BodyConTipoSbagliato_ShouldLanciareJsonException(anno oltre int)")]
    public void Mesi_BodyConTipoSbagliato_ShouldLanciareJsonException(string json)
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<InvioSapMesiRequest>(json, Web));

    // ---- Regola sull'anno: la stessa della griglia ---------------------------------------------

    [Test]
    public void Mesi_Map_AnnoValido_ShouldPortareLAnnoNellaQuery()
        => Assert.That(new InvioSapMesiRequest { Anno = 2003 }.Map(null!).Anno, Is.EqualTo(2003));

    [Test]
    public void Mesi_Map_AnnoCorrentePiuUno_ShouldEssereAmmesso()
    {
        var prossimo = DateTime.UtcNow.Year + 1; // a cavallo di capodanno l'ora italiana puo' essere un anno avanti: resta ammesso
        Assert.That(new InvioSapMesiRequest { Anno = prossimo }.Map(null!).Anno, Is.EqualTo(prossimo));
    }

    [Test]
    public void Mesi_Map_BodyAssente_ShouldLanciareValidationException()
        => Assert.Throws<ValidationException>(() => ((InvioSapMesiRequest?)null).Map(null!));

    [TestCase(null, TestName = "Mesi_Map_AnnoNonValido_ShouldLanciareValidationException(anno assente)")]
    [TestCase(1999, TestName = "Mesi_Map_AnnoNonValido_ShouldLanciareValidationException(sotto il minimo 2000)")]
    [TestCase(0, TestName = "Mesi_Map_AnnoNonValido_ShouldLanciareValidationException(zero)")]
    [TestCase(-2003, TestName = "Mesi_Map_AnnoNonValido_ShouldLanciareValidationException(negativo)")]
    [TestCase(2999, TestName = "Mesi_Map_AnnoNonValido_ShouldLanciareValidationException(oltre anno corrente + 1)")]
    [TestCase(int.MaxValue, TestName = "Mesi_Map_AnnoNonValido_ShouldLanciareValidationException(int.MaxValue)")]
    public void Mesi_Map_AnnoNonValido_ShouldLanciareValidationException(int? anno)
        => Assert.Throws<ValidationException>(() => new InvioSapMesiRequest { Anno = anno }.Map(null!));

    // ---- Filtro Tipologia (POST invio-sap/tipologie, 11/11/2026): anno obbligatorio, mesi facoltativi ----

    [TestCase("""{ "anno": 2026 }""", 2026, null, TestName = "Tipologie_Body_ShouldDeserializzare(solo anno)")]
    [TestCase("""{ "anno": 2026, "mesi": [3, 1] }""", 2026, new[] { 3, 1 }, TestName = "Tipologie_Body_ShouldDeserializzare(anno e mesi)")]
    [TestCase("""{ "ANNO": 2026, "MESI": [] }""", 2026, new int[0], TestName = "Tipologie_Body_ShouldDeserializzare(nomi non case sensitive, mesi vuoti)")]
    [TestCase("""{ "anno": 2026, "tipologieFattura": ["X"], "altro": 1 }""", 2026, null, TestName = "Tipologie_Body_ShouldDeserializzare(campi sconosciuti ignorati)")]
    [TestCase("{}", null, null, TestName = "Tipologie_Body_ShouldDeserializzare(tutto assente)")]
    public void Tipologie_Body_ShouldDeserializzare(string json, int? anno, int[]? mesi)
    {
        var request = JsonSerializer.Deserialize<InvioSapTipologieRequest>(json, Web)!;
        Assert.Multiple(() =>
        {
            Assert.That(request.Anno, Is.EqualTo(anno));
            Assert.That(request.Mesi, Is.EqualTo(mesi));
        });
    }

    [TestCase("""{ "anno": "duemilaventisei" }""", TestName = "Tipologie_BodyConTipoSbagliato_ShouldLanciareJsonException(anno testo)")]
    [TestCase("""{ "anno": 2026, "mesi": 3 }""", TestName = "Tipologie_BodyConTipoSbagliato_ShouldLanciareJsonException(mesi non lista)")]
    [TestCase("""{ "anno": 2026, "mesi": ["marzo"] }""", TestName = "Tipologie_BodyConTipoSbagliato_ShouldLanciareJsonException(mese testo)")]
    [TestCase("""{ "anno": 2026, "mesi": [1.5] }""", TestName = "Tipologie_BodyConTipoSbagliato_ShouldLanciareJsonException(mese decimale)")]
    public void Tipologie_BodyConTipoSbagliato_ShouldLanciareJsonException(string json)
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<InvioSapTipologieRequest>(json, Web));

    [Test]
    public void Tipologie_Map_SoloAnno_ShouldLasciareIMesiNull()
    {
        var query = new InvioSapTipologieRequest { Anno = 2003 }.Map(null!);
        Assert.Multiple(() =>
        {
            Assert.That(query.Anno, Is.EqualTo(2003));
            Assert.That(query.Mesi, Is.Null, "senza mesi la query non filtra per mese");
        });
    }

    [Test]
    public void Tipologie_Map_MesiVuoti_ShouldValereComeAssenti()
        => Assert.That(new InvioSapTipologieRequest { Anno = 2003, Mesi = [] }.Map(null!).Mesi, Is.Null);

    [Test]
    public void Tipologie_Map_Mesi_ShouldPassareNellaQuery()
        => Assert.That(new InvioSapTipologieRequest { Anno = 2003, Mesi = [3, 1] }.Map(null!).Mesi, Is.EqualTo(new[] { 3, 1 }));

    [Test]
    public void Tipologie_Map_BodyAssente_ShouldLanciareValidationException()
        => Assert.Throws<ValidationException>(() => ((InvioSapTipologieRequest?)null).Map(null!));

    [TestCase(null, TestName = "Tipologie_Map_AnnoNonValido_ShouldLanciareValidationException(anno assente)")]
    [TestCase(1999, TestName = "Tipologie_Map_AnnoNonValido_ShouldLanciareValidationException(sotto il minimo 2000)")]
    [TestCase(2999, TestName = "Tipologie_Map_AnnoNonValido_ShouldLanciareValidationException(oltre anno corrente + 1)")]
    public void Tipologie_Map_AnnoNonValido_ShouldLanciareValidationException(int? anno)
        => Assert.Throws<ValidationException>(() => new InvioSapTipologieRequest { Anno = anno, Mesi = [1] }.Map(null!));

    [TestCase(new[] { 0 }, TestName = "Tipologie_Map_MesiNonValidi_ShouldLanciareValidationException(mese zero)")]
    [TestCase(new[] { 13 }, TestName = "Tipologie_Map_MesiNonValidi_ShouldLanciareValidationException(mese 13)")]
    [TestCase(new[] { -1 }, TestName = "Tipologie_Map_MesiNonValidi_ShouldLanciareValidationException(mese negativo)")]
    [TestCase(new[] { 2, 2 }, TestName = "Tipologie_Map_MesiNonValidi_ShouldLanciareValidationException(mese ripetuto)")]
    public void Tipologie_Map_MesiNonValidi_ShouldLanciareValidationException(int[] mesi)
        => Assert.Throws<ValidationException>(() => new InvioSapTipologieRequest { Anno = 2003, Mesi = [.. mesi] }.Map(null!));
}
