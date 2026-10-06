using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Extensions;

public static class AppIoFinancialReportExtensions
{
    /// <summary>
    /// Raggruppa le righe per documento (contratto, trimestre, numero): la testata viene dalla prima
    /// riga del documento, ogni riga diventa una posizione. Conserva l'ordine delle righe in ingresso,
    /// sia dei documenti sia delle posizioni.
    /// </summary>
    /// <param name="righe">Le righe lette dal DB, ordinate per documento e progressivo di riga.</param>
    /// <returns>La lista dei documenti con il loro numero.</returns>
    public static AppIoFinancialReportListDto Raggruppa(this IEnumerable<AppIoFinancialReportRigaDto>? righe)
    {
        var documenti = new Dictionary<string, AppIoFinancialReportDto>();
        foreach (var riga in righe ?? [])
        {
            var key = $"{riga.ContractId}|{riga.YearQuarter}|{riga.Numero}";
            if (!documenti.TryGetValue(key, out var documento))
            {
                documento = new AppIoFinancialReportDto
                {
                    Name = riga.Name,
                    ContractId = riga.ContractId,
                    TipoDoc = riga.TipoDoc,
                    VatCode = riga.VatCode,
                    Valuta = riga.Valuta,
                    Id = riga.Id,
                    Numero = riga.Numero,
                    Data = riga.Data,
                    YearQuarter = riga.YearQuarter
                };
                documenti.Add(key, documento);
            }

            documento.Posizioni.Add(new AppIoFinancialReportPosizioneDto
            {
                Category = riga.Category,
                ProgressivoRiga = riga.ProgressivoRiga,
                CodiceArticolo = riga.CodiceArticolo,
                DescrizioneRiga = riga.DescrizioneRiga,
                Quantita = riga.Quantita,
                Importo = riga.Importo,
                CodIva = riga.CodIva
            });
        }

        // Dictionary conserva l'ordine di inserimento finché non si rimuovono chiavi
        return new AppIoFinancialReportListDto
        {
            FinancialReports = [.. documenti.Values],
            Count = documenti.Count
        };
    }
}
