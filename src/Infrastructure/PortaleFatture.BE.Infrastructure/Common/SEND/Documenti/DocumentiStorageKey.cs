namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti;

public sealed class DocumentiSASStorageKey(
    string? contractId,
    string? quarter,
    string? tipologiaDocumento)
{
    private const string _S3Prefix = @"https://public.pdnd.pagopa.it/apps/public/invoices/";
    public string? ContractId { get; init; } = contractId;
    public string? Quarter { get; init; } = quarter;
    public string? TipologiaDocumento { get; init; } = tipologiaDocumento;  
    public override string ToString()
    {
        return $"{ContractId}/{Quarter}-{TipologiaDocumento}.csv";
    }

    public static DocumentiSASStorageKey Deserialize(string linkDocumento)
    {
        linkDocumento = linkDocumento.Replace(_S3Prefix, string.Empty);
        var dati = linkDocumento!.Split("/");
        var contractId = dati[0];
        var partial = dati[1].Split("-");
        var quarter = $"{partial[0]}-{partial[1]}";
        var tipologiaDocumento = partial[2].Replace(".csv", string.Empty);
        return new DocumentiSASStorageKey(contractId, quarter, tipologiaDocumento);
    } 
    public static string FileName(DocumentiSASStorageKey linkDocumento)
    {
        return $"{linkDocumento}";
    }
}