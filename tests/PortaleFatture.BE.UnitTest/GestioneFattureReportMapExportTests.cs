using NUnit.Framework.Legacy;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Unit test PURI (nessun DB) su FattureExtensions.MapExport: la proiezione
/// GestioneFattureReportDto -> GestioneFattureReportExcelDto usata per il foglio "Non Fatturate".
/// Qui vive il valore-unit-test della feature: e' l'unico pezzo con logica isolabile. Il mapping
/// query->DTO invece e' coperto dagli integration test (serve un DB reale).
/// </summary>
public class GestioneFattureReportMapExportTests
{
    private static GestioneFattureReportDto Pieno() => new()
    {
        IdEnte = "11111111-1111-1111-1111-111111111111",
        RagioneSociale = "Ente Test",
        IdContratto = "contratto-1",
        TipologiaFattura = "PRIMO SALDO",
        NumeroFattura = 2607032716L,
        TipoDocumento = "TD01",
        Anno = 2026,
        Mese = 7,
        TotaleNotificheAnalogiche = 7,
        TotaleNotificheDigitali = 8,
        TotaleNotifiche = 15,
        TotaleImponibileAnalogico = 10.50m,
        TotaleImponibileDigitale = 20.25m,
        TotaleImponibile = 30.75m,
        TotaleIvatoAnalogico = null,
        TotaleIvatoDigitale = null,
        TotaleIvato = null,
        Firmata = "Firmata",
        TotaleFatturaImponibile = 1234.56m,
        TipoContratto = "PAC",
        Stato = "POSTICIPATA"
    };

    [Test]
    public void MapExport_MappaTutteLeColonne_UnoAUno()
    {
        var x = Pieno();
        var r = new[] { x }.MapExport().Single();

        Assert.Multiple(() =>
        {
            Assert.That(r.IdEnte, Is.EqualTo(x.IdEnte));
            Assert.That(r.RagioneSociale, Is.EqualTo(x.RagioneSociale));
            Assert.That(r.IdContratto, Is.EqualTo(x.IdContratto));
            Assert.That(r.TipologiaFattura, Is.EqualTo(x.TipologiaFattura));
            Assert.That(r.NumeroFattura, Is.EqualTo(x.NumeroFattura));
            Assert.That(r.TipoDocumento, Is.EqualTo(x.TipoDocumento));
            Assert.That(r.Anno, Is.EqualTo(x.Anno));
            Assert.That(r.Mese, Is.EqualTo(x.Mese));
            Assert.That(r.TotaleNotificheAnalogiche, Is.EqualTo(x.TotaleNotificheAnalogiche));
            Assert.That(r.TotaleNotificheDigitali, Is.EqualTo(x.TotaleNotificheDigitali));
            Assert.That(r.TotaleNotifiche, Is.EqualTo(x.TotaleNotifiche));
            Assert.That(r.TotaleImponibileAnalogico, Is.EqualTo(x.TotaleImponibileAnalogico));
            Assert.That(r.TotaleImponibileDigitale, Is.EqualTo(x.TotaleImponibileDigitale));
            Assert.That(r.TotaleImponibile, Is.EqualTo(x.TotaleImponibile));
            Assert.That(r.TotaleIvatoAnalogico, Is.EqualTo(x.TotaleIvatoAnalogico));
            Assert.That(r.TotaleIvatoDigitale, Is.EqualTo(x.TotaleIvatoDigitale));
            Assert.That(r.TotaleIvato, Is.EqualTo(x.TotaleIvato));
            Assert.That(r.Firmata, Is.EqualTo(x.Firmata));
            Assert.That(r.TotaleFatturaImponibile, Is.EqualTo(x.TotaleFatturaImponibile));
            Assert.That(r.TipoContratto, Is.EqualTo(x.TipoContratto));
            Assert.That(r.Stato, Is.EqualTo(x.Stato));
        });
    }

    [Test]
    public void MapExport_TuttiICampiNull_NonEsplode_ERestituisceNull()
    {
        // Forma predominante nei dati reali: posticipata senza fattura generata.
        var vuoto = new GestioneFattureReportDto();
        var r = new[] { vuoto }.MapExport().Single();

        Assert.Multiple(() =>
        {
            Assert.That(r.IdEnte, Is.Null);
            Assert.That(r.NumeroFattura, Is.Null);
            Assert.That(r.TotaleImponibile, Is.Null);
            Assert.That(r.TotaleFatturaImponibile, Is.Null);
            Assert.That(r.Firmata, Is.Null);
            Assert.That(r.Stato, Is.Null);
        });
    }

    [Test]
    public void MapExport_ListaVuota_RestituisceVuoto()
    {
        ClassicAssert.IsEmpty(Array.Empty<GestioneFattureReportDto>().MapExport());
    }

    // --- adversarial: la proiezione deve trasportare i valori integri, senza NRE ne' troncamenti ---

    [Test]
    public void MapExport_NumeroFatturaEImportiEstremi_MappatiIntegri()
    {
        var x = Pieno();
        x.NumeroFattura = long.MaxValue;
        x.TotaleFatturaImponibile = decimal.MaxValue;
        x.TotaleImponibile = -99999999.99m;

        var r = new[] { x }.MapExport().Single();
        Assert.Multiple(() =>
        {
            Assert.That(r.NumeroFattura, Is.EqualTo(long.MaxValue), "long non deve overfloware");
            Assert.That(r.TotaleFatturaImponibile, Is.EqualTo(decimal.MaxValue));
            Assert.That(r.TotaleImponibile, Is.EqualTo(-99999999.99m), "importi negativi ammessi");
        });
    }

    [Test]
    public void MapExport_RagioneSocialeConFormulaEChar_TrasportataLetterale()
    {
        // La proiezione NON deve interpretare ne' sanitizzare: e' un mapping. La difesa da
        // CSV/formula-injection, se serve, va all'atto della scrittura Excel, non qui.
        // Questo test fissa il contratto attuale: il valore passa inalterato.
        var x = Pieno();
        x.RagioneSociale = "=1+1";
        x.IdContratto = "@SUM(A1:A9)";
        x.TipoDocumento = "a\"b;c\nd\t<x>";

        var r = new[] { x }.MapExport().Single();
        Assert.Multiple(() =>
        {
            Assert.That(r.RagioneSociale, Is.EqualTo("=1+1"));
            Assert.That(r.IdContratto, Is.EqualTo("@SUM(A1:A9)"));
            Assert.That(r.TipoDocumento, Is.EqualTo("a\"b;c\nd\t<x>"));
        });
    }
}
