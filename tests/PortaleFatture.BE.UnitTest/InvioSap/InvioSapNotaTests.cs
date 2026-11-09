using System.Text.Json;
using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// Nota di un invio (<c>pfw.DettaglioInvioSAP.Nota</c>) e suo aggregato nelle viste (<c>NoteInvio</c>, <c>Note</c>).
/// I due formati di data e le due azioni sono quelli osservati in DEV (C7, be-invio-sap.md §5.4).
/// </summary>
[Category("InvioSap")]
public class InvioSapNotaTests
{
    private const string AggregatoDev =
        "[{\"Nota\":{\"Data\":\"2093-03-01 09:00:00.147\",\"Testo\":\"Migrazione\",\"Azione\":\"GeneraJson\"}}," +
        "{\"Nota\":{\"Data\":\"2093-04-03T09:00:00.1234567\",\"Testo\":\"Re-invio\",\"Azione\":\"GeneraJsonFatture\"}}]";

    [Test]
    public void Aggregato_ConIDueFormatiDiData_ShouldEssereLetto()
    {
        var note = InvioSapNota.ParseNoteInvio(AggregatoDev);
        Assert.Multiple(() =>
        {
            Assert.That(note, Has.Count.EqualTo(2));
            Assert.That(note[0].Data, Is.EqualTo(new DateTime(2093, 3, 1, 9, 0, 0, 147, DateTimeKind.Utc)));
            Assert.That(note[0].Azione, Is.EqualTo("GeneraJson"));
            Assert.That(note[1].Data, Is.EqualTo(new DateTime(2093, 4, 3, 9, 0, 0, DateTimeKind.Utc).AddTicks(1234567)));
            Assert.That(note[1].Testo, Is.EqualTo("Re-invio"));
            Assert.That(note[1].Azione, Is.EqualTo("GeneraJsonFatture"));
        });
    }

    [TestCase(null, TestName = "Aggregato_NonLeggibile_ShouldDareListaVuota_SenzaEccezioni(Aggregato: null)")]
    [TestCase("", TestName = "Aggregato_NonLeggibile_ShouldDareListaVuota_SenzaEccezioni(Aggregato: vuoto)")]
    [TestCase("   ", TestName = "Aggregato_NonLeggibile_ShouldDareListaVuota_SenzaEccezioni(Aggregato: spazi)")]
    [TestCase("[]", TestName = "Aggregato_NonLeggibile_ShouldDareListaVuota_SenzaEccezioni(Aggregato: array vuoto)")]
    [TestCase("{\"Nota\":{}}", TestName = "Aggregato_NonLeggibile_ShouldDareListaVuota_SenzaEccezioni(Aggregato: oggetto, non array)")]
    [TestCase("[{\"Nota\":", TestName = "Aggregato_NonLeggibile_ShouldDareListaVuota_SenzaEccezioni(Aggregato: troncato)")]
    [TestCase("non json", TestName = "Aggregato_NonLeggibile_ShouldDareListaVuota_SenzaEccezioni(Aggregato: testo)")]
    public void Aggregato_NonLeggibile_ShouldDareListaVuota_SenzaEccezioni(string? json)
    {
        Assert.That(InvioSapNota.ParseNoteInvio(json), Is.Empty);
    }

    [Test]
    public void CampiMancanti_ShouldRestareNull()
    {
        var note = InvioSapNota.ParseNoteInvio("[{\"Nota\":{\"Testo\":\"solo testo\"}}]");
        Assert.Multiple(() =>
        {
            Assert.That(note, Has.Count.EqualTo(1));
            Assert.That(note[0].Data, Is.Null);
            Assert.That(note[0].Azione, Is.Null);
            Assert.That(note[0].Testo, Is.EqualTo("solo testo"));
        });
    }

    [Test]
    public void DataIllegibile_ShouldRestareNull_SenzaPerdereLaNota()
    {
        var note = InvioSapNota.ParseNoteInvio("[{\"Nota\":{\"Data\":\"31/12/2093\",\"Testo\":\"t\"}}]");
        Assert.That(note.Single().Data, Is.Null);
    }

    [Test]
    public void ElementoNull_ShouldEssereSaltato()
    {
        var note = InvioSapNota.ParseNoteInvio("[{\"Nota\":null},{\"Nota\":{\"Testo\":\"ok\"}}]");
        Assert.That(note.Select(n => n.Testo), Is.EqualTo(new[] { "ok" }));
    }

    [Test]
    public void NotaSalvataComeStringaJson_ShouldEssereRiletta()
    {
        var note = InvioSapNota.ParseNoteInvio("[{\"Nota\":\"{\\\"Testo\\\":\\\"annidata\\\"}\"}]");
        Assert.That(note.Single().Testo, Is.EqualTo("annidata"));
    }

    [Test]
    public void ToJson_ShouldScrivereIlFormatoDiData_InIsoUtc()
    {
        var json = new InvioSapNota(new DateTime(2093, 11, 5, 14, 45, 0, DateTimeKind.Utc), "Testo", InvioSapRegole.DescrizioneAzione).ToJson();
        using var doc = JsonDocument.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.That(doc.RootElement.GetProperty("Data").GetString(), Is.EqualTo("2093-11-05T14:45:00.0000000"));
            Assert.That(doc.RootElement.GetProperty("Testo").GetString(), Is.EqualTo("Testo"));
            Assert.That(doc.RootElement.GetProperty("Azione").GetString(), Is.EqualTo("GeneraJsonFatture"));
        });
    }

    [Test]
    public void RoundTrip_ConCaratteriSpeciali_ShouldConservareIlTesto()
    {
        const string testo = "Nota con \"apici\", \\ barre, a capo\n, <tag> & emoji 🚀";
        var originale = new InvioSapNota(new DateTime(2093, 1, 2, 3, 4, 5, DateTimeKind.Utc), testo, "GeneraJsonFatture");
        var aggregato = $"[{{\"Nota\":{originale.ToJson()}}}]";

        var riletta = InvioSapNota.ParseNoteInvio(aggregato).Single();
        Assert.That(riletta, Is.EqualTo(originale));
    }

    [Test]
    public void ToJson_ConTestoNull_ShouldEssereJsonValido()
    {
        var json = new InvioSapNota(null, null, null).ToJson();
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("Testo").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }
}
