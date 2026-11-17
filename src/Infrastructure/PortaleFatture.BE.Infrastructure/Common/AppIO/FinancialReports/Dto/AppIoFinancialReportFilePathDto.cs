namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;

/// <summary>
/// Percorso sul blob storage del file del financial report di un trimestre. Uso interno del download:
/// non va restituito al client.
/// </summary>
public sealed class AppIoFinancialReportFilePathDto
{
    /// <summary>Trimestre nel formato 'AAAA_T'.</summary>
    public string YearQuarter { get; set; } = string.Empty;

    /// <summary>Il valore di file_path.</summary>
    public string FilePath { get; set; } = string.Empty;
}
