using System.Data;
using System.IO.Compression;
using System.Text.RegularExpressions;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Infrastructure.Common.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Gateway.Storage;

namespace PortaleFatture.BE.Api.Modules.Fatture.Extensions;

public static class FattureExtensions
{
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
            else
                dataSet.Tables.Add(commesse[i]!.FillTableWithTotalsRel(0, $"Commesse non fatt. {month}"));
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

    public static FattureQueryRicerca Map(this FatturaRicercaRequest req, AuthenticationInfo authInfo)
    {
        return new FattureQueryRicerca(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = req.IdEnti,
            TipologiaFattura = req.TipologiaFattura
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
        using var memoryStreamZip = new MemoryStream();
        {
            using var zipArchive = new ZipArchive(memoryStreamZip, ZipArchiveMode.Create, false);
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
            zipBytes = memoryStreamZip.ToArray();
            memoryStreamZip.Flush();
        }
        return zipBytes;
    }
}