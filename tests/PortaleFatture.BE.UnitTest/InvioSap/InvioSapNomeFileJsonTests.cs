using System.Text.RegularExpressions;
using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// Il nome del file JSON e' parte del contratto BE-DATA (RI-9, contracts/data-process.md): lo genera il
/// backend, lo scrive nel Registro, lo mostra in Messaggi e DATA deve depositare il blob con quel nome.
/// Formato: <c>sap-yyyyMMdd-HHmmss-&lt;batchId&gt;.json</c>, data in orario italiano.
/// </summary>
[Category("InvioSap")]
public class InvioSapNomeFileJsonTests
{
    private static readonly Regex Formato = new(
        @"^sap-\d{8}-\d{6}-[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\.json$", RegexOptions.Compiled);

    [Test]
    public void NomeFile_ShouldRispettareIlFormatoDelContratto()
    {
        var nome = InvioSapRegole.NomeFileJson(new DateTime(2026, 10, 13, 7, 30, 0, DateTimeKind.Utc), Guid.NewGuid());

        Assert.That(nome, Does.Match(Formato));
    }

    [Test]
    public void NomeFile_ShouldUsareOrarioItaliano_NonUtc()
    {
        // 13/10/2026 07:30 UTC = 09:30 ora legale italiana: la convenzione del progetto e' Europe/Rome.
        var nome = InvioSapRegole.NomeFileJson(new DateTime(2026, 10, 13, 7, 30, 0, DateTimeKind.Utc), Guid.Empty);

        Assert.That(nome, Does.StartWith("sap-20261013-093000-"));
    }

    [Test]
    public void NomeFile_AMezzanotteUtc_ShouldCambiareGiorno()
    {
        // 31/12 23:30 UTC e' gia' il 1/1 in Italia: il giorno nel nome deve essere quello italiano.
        var nome = InvioSapRegole.NomeFileJson(new DateTime(2026, 12, 31, 23, 30, 0, DateTimeKind.Utc), Guid.Empty);

        Assert.That(nome, Does.StartWith("sap-20270101-003000-"));
    }

    [Test]
    public void NomeFile_ShouldContenereIlBatchIdMinuscolo()
    {
        var batchId = Guid.Parse("7F3C2A9E-5D1B-4E0A-9C33-1B2F6A8D4E10");
        var nome = InvioSapRegole.NomeFileJson(DateTime.UtcNow, batchId);

        Assert.That(nome, Does.EndWith("-7f3c2a9e-5d1b-4e0a-9c33-1b2f6a8d4e10.json"));
    }

    [Test]
    public void NomeFile_DueBatch_ShouldEssereDiversi_AncheNelloStessoSecondo()
    {
        var istante = new DateTime(2026, 10, 27, 10, 0, 0, DateTimeKind.Utc);
        var a = InvioSapRegole.NomeFileJson(istante, Guid.NewGuid());
        var b = InvioSapRegole.NomeFileJson(istante, Guid.NewGuid());

        Assert.That(a, Is.Not.EqualTo(b), "L'unicita' la garantisce il batchId, non l'orario.");
    }

    [Test]
    public void NomeFile_ShouldEssereSicuroPerBlobStorage()
    {
        // Niente spazi, due punti o caratteri che un path di blob non gradisce; lunghezza contenuta.
        var nome = InvioSapRegole.NomeFileJson(DateTime.UtcNow, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(nome, Does.Not.Contain(" ").And.Not.Contain(":").And.Not.Contain("/").And.Not.Contain("\\"));
            Assert.That(nome, Has.Length.LessThan(80));
        });
    }
}
