namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti;
public sealed class DocumentiFinancialReportSASStorageKey(
    string? contractId,
    string? quarter,
    string? tipologiaDocumento)
{
    public const string S3Prefix = @"https://public.pdnd.pagopa.it/apps/public/invoices/";
    public const string AzurePrefix = @"blob.core.windows.net";
    public string? ContractId { get; init; } = contractId;
    public string? Quarter { get; init; } = quarter;
    public string? TipologiaDocumento { get; init; } = tipologiaDocumento;  
    public override string ToString()
    {
        return $"{ContractId}/{Quarter}-{TipologiaDocumento}.csv";
    }

    private static string RemoveSignatureFromBlobUrl(string urlWithSas)
    {
        if (string.IsNullOrEmpty(urlWithSas))
            return urlWithSas; 
        var uri = new Uri(urlWithSas); 
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}"; 
    }


    public static DocumentiFinancialReportSASStorageKey DeserializeBare(string linkDocumento)
    {

        linkDocumento = RemoveSignatureFromBlobUrl(linkDocumento);
        var uri = new Uri(linkDocumento);
        var path = uri.AbsolutePath;
        linkDocumento = path.Replace("/invoices/", string.Empty);
        var dati = linkDocumento.Split("/");
        var contractId = dati[0]; // pagopa-psp-01
        var partial = dati[1].Split("-");
        var quarter = $"{partial[0]}-{partial[1]}"; // 2024-q3
        var tipologiaDocumento = partial[2].Replace(".csv", string.Empty); // DetailedReport
        return new DocumentiFinancialReportSASStorageKey(contractId, quarter, tipologiaDocumento);
    }
    public static DocumentiFinancialReportSASStorageKey Deserialize(string linkDocumento)
    {
        linkDocumento = linkDocumento.Replace(S3Prefix, string.Empty); 
        var dati = linkDocumento!.Split("/");
        var contractId = dati[0];
        var partial = dati[1].Split("-");
        var quarter = $"{partial[0]}-{partial[1]}";
        var tipologiaDocumento = partial[2].Replace(".csv", string.Empty);
        return new DocumentiFinancialReportSASStorageKey(contractId, quarter, tipologiaDocumento);
    } 
    public static string FileName(DocumentiFinancialReportSASStorageKey linkDocumento)
    {
        return $"{linkDocumento}";
    }
}