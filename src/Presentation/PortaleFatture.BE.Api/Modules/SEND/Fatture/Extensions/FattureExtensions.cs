using System.Data;
using System.IO.Compression;
using System.Reflection;
using MediatR;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Api.Modules.Fatture;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Messaggi;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands;
using PortaleFatture.BE.Infrastructure.Gateway.Storage;
namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;

public static class FattureExtensions
{
    public static FattureQueryRicercaByEnte Map(this FatturaRicercaEnteRequest req, AuthenticationInfo authInfo)
    {
        return new FattureQueryRicercaByEnte(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            TipologiaFattura = req.TipologiaFattura
        };
    }

    public static FatturaRiptristinoSAPCommand Map2(this FatturaPipelineSapRequest request, AuthenticationInfo authInfo, bool invio)
    {
        return new FatturaRiptristinoSAPCommand(authInfo, request.AnnoRiferimento!.Value, request.MeseRiferimento!.Value, request.TipologiaFattura)
        {
            Invio = invio
        };
    }

    public static FatturaInvioSap Map(this FatturaPipelineSapRequest request)
    {
        return new FatturaInvioSap()
        {
            AnnoRiferimento = request.AnnoRiferimento!.Value,
            MeseRiferimento = request.MeseRiferimento!.Value,
            TipologiaFattura = request.TipologiaFattura
        };
    }

    public static Dictionary<string, object> ToDictionary<T>(this T obj)
    {
        var dict = new Dictionary<string, object>();
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }
        var type = typeof(T);
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = property.GetValue(obj);
            dict[property.Name] = value!;
        }
        return dict;
    }

    public static async Task<Dictionary<string, byte[]>> ReportFatture(this FatturaRicercaRequest request, IMediator handler, AuthenticationInfo authInfo)
    {
        Dictionary<string, byte[]> reports = [];

        if(request.TipologiaFattura!.IsNullNotAny())
        {
            request.TipologiaFattura = (await handler.Send(new FattureTipologiaAnniMeseQuery(authInfo)
            {
                Anno = request.Anno!,
                Mese = request.Mese!
            }))!.ToArray();
        } 

        foreach (var tipologia in request.TipologiaFattura!)
        {
            var month = request.Mese.GetMonth();
            var year = request.Anno;
            switch (tipologia)
            {
                case TipologiaFattura.PRIMOSALDO:
                    var fatture = await handler.Send(request.Mapv2(authInfo, tipologia));
                    if (fatture.IsNotEmpty())
                        reports.Add($"Lista {tipologia} {year} {month}", fatture!.ReportFattureRel(month, tipologia));
                    break;
                case TipologiaFattura.SECONDOSALDO:
                    fatture = await handler.Send(request.Mapv2(authInfo, tipologia));
                    if (fatture.IsNotEmpty())
                        reports.Add($"Lista {tipologia} {year} {month}", fatture!.ReportFattureRel(request.Mese.GetMonth(), tipologia));
                    break;
                case TipologiaFattura.ANTICIPO:
                    var commesse = await handler.Send(request.Mapv3(authInfo));
                    if (commesse.IsNotEmpty())
                        reports.Add($"Lista ANTICIPO {year} {month}", commesse!.ReportFattureModuloCommessa(request.Mese.GetMonth()));
                    break;
                case TipologiaFattura.ACCONTO:
                    var acconto = await handler.Send(request.Mapv4(authInfo));
                    if (acconto.IsNotEmpty())
                        reports.Add($"Lista ACCONTO {year} {month}", acconto!.ReportFattureAcconto(request.Mese.GetMonth()));
                    break;
                default:
                    break;
            }
        }
        return reports;
    }

    public static bool IsNotEmpty<T>(this List<IEnumerable<T>>? model)
    {
        if (model == null)
            return false;
        if (model.Count == 0)
            return false;
        var count = 0;
        foreach (var item in model)
            count += item.Count();

        if (count == 0)
            return false;
        return true;
    }

    public static byte[] ReportFattureModuloCommessa(this List<IEnumerable<FattureCommessaExcelDto>> commesse, string month)
    {
        DataSet? dataSet = new();
        for (var i = 0; i < commesse.Count; i++)
        {
            if (i == 0)
                dataSet.Tables.Add(commesse[i]!.FillTableWithTotalsRel(0, $"Commesse {month}"));
            else if (i == 1)
                dataSet.Tables.Add(commesse[i]!.FillTableWithTotalsRel(0, $"Commesse stimate {month}"));
            else if (i == 2)
                dataSet.Tables.Add(commesse[i]!.FillTableWithTotalsRel(0, $"Commesse stimate fatt. {month}"));
            else if (i == 3)
                dataSet.Tables.Add(commesse[i]!.FillTableWithTotalsRel(0, $"Commesse fatt. {month}"));
            else if (i == 4)
                dataSet.Tables.Add(commesse[i]!.FillTableWithTotalsRel(0, $"Commesse non fatt. {month}"));
            else
                dataSet.Tables.Add(commesse[i]!.FillTableWithTotalsRel(0, $"Commesse eliminate fatt. {month}"));
        }

        using var memory = dataSet!.ToExcel();
        return memory.ToArray();
    }

    public static byte[] ReportFattureAcconto(this List<IEnumerable<FattureAccontoExcelDto>> commesse, string month)
    {
        DataSet? dataSet = new();
        for (var i = 0; i < commesse.Count; i++)
            dataSet.Tables.Add(commesse[i]!.FillTableWithTotalsRel(0, $"Acconto {month}"));
        using var memory = dataSet!.ToExcel();
        return memory.ToArray();
    }

    public static byte[] ReportFattureRel(this List<IEnumerable<FattureRelExcelDto>> fatture, string month, string tipologia)
    {
        DataSet? dataSet = new();
        for (var i = 0; i < fatture.Count; i++)
        {
            if (i == 0)
                dataSet.Tables.Add(fatture[i]!.FillTableWithTotalsRel(9, $"Regolari Esecuzioni {month}"));
            else if (i == 1)
                dataSet.Tables.Add(fatture[i]!.FillTableWithTotalsRel(9, $"Enti Fatturabili  {month}"));
            else
                dataSet.Tables.Add(fatture[i]!.FillTableWithTotalsRel(9, $"Note di Credito {month}"));
        }

        using var memory = dataSet!.ToExcel();
        return memory.ToArray();
    }

    public static (MessaggioCreateCommand, DocumentiStorageKey) Mapv2(this FatturaRicercaRequest req, AuthenticationInfo authInfo, string? contentType, string? contentLanguage)
    {
        var command = new MessaggioCreateCommand(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            Json = req.Serialize(),
            TipologiaDocumento = TipologiaDocumento.Fatturazione,
            ContentType = contentType,
            ContentLanguage = contentLanguage
        };

        var key = new DocumentiStorageKey(authInfo.IdEnte, authInfo.Id, TipologiaDocumento.Fatturazione, command.DataInserimento.Year, command.Hash);
        command.LinkDocumento = key.ToString();
        return (command, key);
    }

    public static FattureQueryRicerca Map(this FatturaRicercaRequest req, AuthenticationInfo authInfo)
    {
        return new FattureQueryRicerca(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = req.IdEnti,
            TipologiaFattura = req.TipologiaFattura,
            Cancellata = req.Cancellata == null ? false : req.Cancellata.Value
        };
    }
    public static FattureRelExcelQuery Mapv2(this FatturaRicercaRequest req, AuthenticationInfo authInfo, string tipologiaFattura)
    {
        return new FattureRelExcelQuery(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = req.IdEnti,
            TipologiaFattura = tipologiaFattura
        };
    }

    public static FattureCommessaExcelQuery Mapv3(this FatturaRicercaRequest req, AuthenticationInfo authInfo)
    {
        return new FattureCommessaExcelQuery(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = req.IdEnti
        };
    }

    public static FattureAccontoExcelQuery Mapv4(this FatturaRicercaRequest req, AuthenticationInfo authInfo)
    {
        return new FattureAccontoExcelQuery(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = req.IdEnti
        };
    }

    public static IEnumerable<FattureExcel> Map(this FattureListaDto model)
    {
        var result = new List<FattureExcel>();
        foreach (var item in model)
        {
            foreach (var pos in item.fattura!.Posizioni!)
            {
                result.Add(new FattureExcel()
                {
                    Numero = item.fattura!.Numero,
                    Posizione = pos.CodiceMateriale,
                    Totale = pos.Imponibile.ToString("0.00"),
                    PeriodoRiferimento = pos.PeriodoRiferimento
                });
            }
            result.Add(new FattureExcel()
            {
                Causale = item.fattura!.Causale,
                DataFattura = item.fattura!.DataFattura,
                Divisa = item.fattura!.Divisa,
                IdContratto = item.fattura!.IdContratto,
                Numero = item.fattura!.Numero,
                IstitutioID = item.fattura.IstitutioID,
                MetodoPagamento = item.fattura.MetodoPagamento,
                OnboardingTokenID = item.fattura.OnboardingTokenID,
                Prodotto = item.fattura.Prodotto,
                RagioneSociale = item.fattura.RagioneSociale,
                TipologiaFattura = item.fattura.TipologiaFattura,
                TipoContratto = item.fattura.TipoContratto,
                Totale = item.fattura.Totale.ToString("0.00"),
                Identificativo = item.fattura.Identificativo,
                Sollecito = item.fattura.Sollecito,
                Split = item.fattura.Split,
                TipoDocumento = item.fattura.TipoDocumento,
                Posizione = "totale:",
            });

            result.Add(new FattureExcel());
        }
        return result;
    }

    public static byte[] CreateZip(this
    Dictionary<string, byte[]> reports,
    ILogger<FattureModule> logger,
    string extension = ".xlsx")
    {
        byte[] zipBytes;
        using var memoryStreamZip = reports.CreateMemoryStreamZip(logger);
        {
            zipBytes = memoryStreamZip.ToArray();
            memoryStreamZip.Flush();
        }
        return zipBytes;
    }

    public static MemoryStream CreateMemoryStreamZip(this
        Dictionary<string, byte[]> reports,
        ILogger<FattureModule> logger,
        string extension = ".xlsx")
    {

        var memoryStreamZip = new MemoryStream();
        using var zipArchive = new ZipArchive(memoryStreamZip, ZipArchiveMode.Create, leaveOpen: true);
        {
            memoryStreamZip.Position = 0;
            foreach (var report in reports!)
            {
                try
                {
                    zipArchive.AddFile(report.Key + extension, report.Value);
                }
                catch
                {
                    var msg = $"Errore nel legger il file {report.Key}!";
                    logger.LogError(msg);
                }
            }
            zipArchive.Dispose();
        }
        return memoryStreamZip;
    }
}