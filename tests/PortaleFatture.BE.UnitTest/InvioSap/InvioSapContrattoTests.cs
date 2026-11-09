using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;
using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// Contratti verso FE e DATA che non toccano il DB: esito del Genera → HTTP (be-invio-sap.md §11), body di
/// <c>createRun</c> (§8.7), colonne degli Excel (§10), mapping delle response (orario italiano, storico).
/// </summary>
[Category("InvioSap")]
public class InvioSapContrattoTests
{
    private static int Status(IResult r) => ((IStatusCodeHttpResult)r).StatusCode ?? 200;

    // =============================================================================================
    // Esito → HTTP
    // =============================================================================================

    [Test]
    public void Ok_ShouldEssere202_ConBatchIntero_InOraItaliana()
    {
        var esito = new InvioSapGeneraEsito
        {
            Tipo = InvioSapGeneraEsitoTipo.Ok, BatchId = 9312, NumeroFatture = 2, IdFatture = [9505, 9506],
            DataAvvioUtc = new DateTime(2093, 11, 5, 14, 45, 0, DateTimeKind.Utc)
        };
        var r = esito.ToResult();
        var body = (InvioSapGeneraResponse)((IValueHttpResult)r).Value!;
        Assert.Multiple(() =>
        {
            Assert.That(Status(r), Is.EqualTo(StatusCodes.Status202Accepted));
            Assert.That(body.BatchId, Is.EqualTo(9312));
            Assert.That(body.Stato, Is.EqualTo("IN_ELABORAZIONE"));
            Assert.That(body.NumeroFatture, Is.EqualTo(2));
            Assert.That(body.IdFatture, Is.EqualTo(new long[] { 9505, 9506 }));
            Assert.That(body.DataAvvio, Is.EqualTo(new DateTime(2093, 11, 5, 15, 45, 0)), "UTC+1 in novembre.");
        });
    }

    [Test]
    public void NonValida_ShouldEssere400_ConGliErroriInTesto()
    {
        var r = InvioSapGeneraEsito.NonValida(["a", "b"]).ToResult();
        Assert.Multiple(() =>
        {
            Assert.That(Status(r), Is.EqualTo(400));
            Assert.That(((IValueHttpResult)r).Value, Is.EqualTo("a; b"));
        });
    }

    [Test]
    public void NonTrovate_ShouldEssere404()
    {
        Assert.That(Status(InvioSapGeneraEsito.NonTrovate([1]).ToResult()), Is.EqualTo(404));
    }

    [TestCase(InvioSapMotivoConflitto.LOCK_ATTIVO)]
    [TestCase(InvioSapMotivoConflitto.LOCK_NON_RILASCIATO)]
    [TestCase(InvioSapMotivoConflitto.GRUPPO_CAMBIATO)]
    public void Conflitto_ShouldEssere409_ConIlMotivo(string motivo)
    {
        var timeout = new DateTime(2093, 7, 1, 10, 0, 0, DateTimeKind.Utc);
        var r = InvioSapGeneraEsito.Conflitto(motivo, 9311, timeout).ToResult();
        var body = (InvioSapConflittoResponse)((IValueHttpResult)r).Value!;
        Assert.Multiple(() =>
        {
            Assert.That(Status(r), Is.EqualTo(409));
            Assert.That(body.Motivo, Is.EqualTo(motivo));
            Assert.That(body.BatchId, Is.EqualTo(9311));
            Assert.That(body.LockTimeout, Is.EqualTo(new DateTime(2093, 7, 1, 12, 0, 0)), "UTC+2 in luglio.");
        });
    }

    [Test]
    public void PipelineFallita_ShouldEssere500_SenzaDettagliTecnici()
    {
        var r = InvioSapGeneraEsito.PipelineFallita().ToResult();
        Assert.Multiple(() =>
        {
            Assert.That(Status(r), Is.EqualTo(500));
            Assert.That(((ProblemHttpResult)r).ProblemDetails.Detail, Does.Contain("riprovare"));
        });
    }

    [Test]
    public void OgniTipoDiEsito_ShouldAvereUnaTraduzione()
    {
        foreach (var tipo in Enum.GetValues<InvioSapGeneraEsitoTipo>())
        {
            var esito = new InvioSapGeneraEsito
            {
                Tipo = tipo, BatchId = 1, NumeroFatture = 0, DataAvvioUtc = DateTime.UtcNow, Motivo = InvioSapMotivoConflitto.LOCK_ATTIVO
            };
            Assert.That(() => esito.ToResult(), Throws.Nothing, tipo.ToString());
        }
    }

    // =============================================================================================
    // Body di createRun (contratto con DATA)
    // =============================================================================================

    [Test]
    public void BodyPipeline_ShouldContenereBatchIdEIdMessaggio_ConQueiNomi()
    {
        var json = JsonSerializer.Serialize(InvioSapPipelineRequest.Crea(9312, 48211, "PRIMO SALDO", 2093, 2));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("batchId").GetInt32(), Is.EqualTo(9312));
            Assert.That(root.GetProperty("idMessaggio").GetInt64(), Is.EqualTo(48211));
            var p = root.GetProperty("parametri")[0];
            Assert.That(p.GetProperty("tipoFattura").GetString(), Is.EqualTo("PRIMO SALDO"));
            Assert.That(p.GetProperty("MesidaFatturare")[0].GetString(), Is.EqualTo("209302"), "Mese a due cifre, formato legacy.");
        });
    }

    // =============================================================================================
    // Response del dettaglio e del lock
    // =============================================================================================

    [Test]
    public void Dettaglio_ShouldMappareStoricoStatoEIdDocumentoVuoto()
    {
        var lista = new InvioSapDettaglioListDto
        {
            Count = 1,
            Fatture =
            [
                new InvioSapDettaglioDto
                {
                    IdFattura = 9503, IdentificativoFattura = "IT-9503", IdDocumento = "", FatturaInviata = true, TotaleFattura = -10.004,
                    DataUltimaGenerazione = new DateTime(2093, 3, 1, 9, 5, 0),
                    NoteInvio = "[{\"Nota\":{\"Data\":\"2093-03-01 09:00:00.147\",\"Testo\":\"Migrazione\",\"Azione\":\"GeneraJson\"}}]"
                }
            ]
        };
        var f = lista.Map().Fatture.Single();
        Assert.Multiple(() =>
        {
            Assert.That(f.IdDocumento, Is.Null, "C11: stringa vuota → null.");
            Assert.That(f.StatoInvio, Is.EqualTo("INVIATE"));
            Assert.That(f.NumeroFattura, Is.EqualTo("IT-9503"));
            Assert.That(f.Importo, Is.EqualTo(-10.00m));
            Assert.That(f.NumeroInvii, Is.EqualTo(1));
            Assert.That(f.StoricoInvii.Single().NInvio, Is.EqualTo(1));
            Assert.That(f.StoricoInvii.Single().Data, Is.EqualTo(new DateTime(2093, 3, 1, 10, 0, 0, 147)), "Ora italiana.");
            Assert.That(f.DataUltimaGenerazione, Is.EqualTo(new DateTime(2093, 3, 1, 10, 5, 0)));
        });
    }

    [Test]
    public void Lock_ShouldEssereInOraItaliana()
    {
        var r = new InvioSapLockDto
        {
            Azione = "GeneraJsonFatture", BatchId = 7,
            LockStartUTC = new DateTime(2093, 1, 1, 10, 0, 0), LockTimeoutUTC = new DateTime(2093, 1, 1, 11, 0, 0)
        }.Map();
        Assert.Multiple(() =>
        {
            Assert.That(r.LockStart, Is.EqualTo(new DateTime(2093, 1, 1, 11, 0, 0)));
            Assert.That(r.LockTimeout, Is.EqualTo(new DateTime(2093, 1, 1, 12, 0, 0)));
        });
    }

    [Test]
    public void Stati_ShouldEssereIQuattroCodiciDelFiltro()
    {
        Assert.That(InvioSapExtensions.Stati().Select(s => s.Codice), Is.EqualTo(FiltroStatoInvioSap.Tutti));
    }

    // =============================================================================================
    // Excel
    // =============================================================================================

    private static string[] Intestazioni<T>() => typeof(T).GetProperties()
        .Select(p => p.GetCustomAttribute<HeaderAttributev2>())
        .Where(a => a is not null)
        .OrderBy(a => a!.Order)
        .Select(a => a!.Caption!)
        .ToArray();

    [Test]
    public void ExcelGriglia_ShouldAvereLeColonneNellOrdine()
    {
        Assert.That(Intestazioni<InvioSapGrigliaExcelDto>(),
            Is.EqualTo(new[] { "Tipologia Fattura", "Anno", "Mese", "Stato", "Numero Fatture", "Importo Totale" }));
    }

    [Test]
    public void ExcelReinviate_ShouldAvereLeColonneNellOrdine()
    {
        Assert.That(Intestazioni<InvioSapReinviataExcelDto>(), Is.EqualTo(new[]
        {
            "Id Fattura", "Progressivo", "Tipo Documento", "Tipologia Fattura", "Id Ente", "Ragione Sociale",
            "Data Fattura", "Totale Fattura", "Anno", "Mese", "Numero Invii", "Note"
        }));
    }

    [Test]
    public void ExcelReinviate_ShouldAppiattireLeNote()
    {
        var riga = InvioSapReinviataExcelDto.Da(new InvioSapReinviataDto
        {
            IdFattura = 9502, DataFattura = new DateTime(2093, 2, 10), MeseRiferimento = 1, ConteggioInvio = 2,
            Note = "[{\"Nota\":{\"Data\":\"2093-03-01 09:00:00.147\",\"Testo\":\"Uno\"}},{\"Nota\":{\"Testo\":\"Due\"}}]"
        });
        Assert.Multiple(() =>
        {
            Assert.That(riga.Note, Is.EqualTo($"01/03/2093 10:00 Uno{Environment.NewLine}Due"));
            Assert.That(riga.DataFattura, Is.EqualTo("10/02/2093"));
        });
    }

    [TestCase("Risultati Invio SAP")]
    [TestCase("Fatture re-inviate")]
    public void NomeFoglio_ShouldStareNei31Caratteri(string foglio)
    {
        Assert.That(foglio.Length, Is.LessThanOrEqualTo(31));
    }
}
