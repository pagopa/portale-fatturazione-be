using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>Vocabolari degli stati: regola di DATA 0 = non inviata, 1 = inviata, NULL = in elaborazione (C2).</summary>
[Category("InvioSap")]
public class InvioSapStatiTests
{
    [Test]
    public void DaFatturaInviata_ShouldSeguireLaRegolaDiData()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StatoInvioSap.DaFatturaInviata(false), Is.EqualTo(StatoInvioSap.DA_INVIARE));
            Assert.That(StatoInvioSap.DaFatturaInviata(true), Is.EqualTo(StatoInvioSap.INVIATE));
            Assert.That(StatoInvioSap.DaFatturaInviata(null), Is.EqualTo(StatoInvioSap.IN_ELABORAZIONE));
        });
    }

    [Test]
    public void Filtro_ShouldAvereQuattroCodici_InOrdine()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FiltroStatoInvioSap.Tutti, Is.EqualTo(new[] { "TUTTE", "DA_INVIARE", "INVIATE", "IN_ELABORAZIONE" }));
            Assert.That(FiltroStatoInvioSap.Descrizioni.Select(d => d.Codice), Is.EqualTo(FiltroStatoInvioSap.Tutti));
            Assert.That(FiltroStatoInvioSap.Descrizioni.All(d => !string.IsNullOrWhiteSpace(d.Descrizione)), Is.True);
        });
    }

    [TestCase(null, "TUTTE")]
    [TestCase("", "TUTTE")]
    [TestCase(" in_elaborazione ", "IN_ELABORAZIONE")]
    [TestCase("Inviate", "INVIATE")]
    public void Normalizza_ShouldIgnorareSpaziEMaiuscole(string? filtro, string atteso)
    {
        Assert.That(FiltroStatoInvioSap.Normalizza(filtro), Is.EqualTo(atteso));
    }

    [Test]
    public void StatiDelBatch_ShouldEssereQuelliDiData()
    {
        Assert.That(StatoBatchSap.Tutti, Is.EqualTo(new[] { "IN_ELABORAZIONE", "COMPLETATO", "ERRORE" }));
    }
}
