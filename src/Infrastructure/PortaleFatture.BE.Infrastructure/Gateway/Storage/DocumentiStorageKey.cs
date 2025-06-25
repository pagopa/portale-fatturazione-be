namespace PortaleFatture.BE.Infrastructure.Gateway.Storage;

public sealed class DocumentiStorageKey(
    string? idEnte,
    string? idUtente,
    string? tipologiaDocumento,
    int annoInserimento,
    string? hash)
{

    public string? IdEnte { get; init; } = idEnte;
    public string? IdUtente { get; init; } = idUtente;
    public string? TipologiaDocumento { get; init; } = tipologiaDocumento;
    public int? Anno { get; init; } = annoInserimento;
    public string? Hash { get; init; } = hash; 

    public override string ToString()
    {
        return $"{IdEnte}_{IdUtente}_{TipologiaDocumento!.Replace(" ", "-")}_{Anno}_{Hash}";
    }

    public static DocumentiStorageKey Deserialize(string linkDocumento)
    { 
        var dati = linkDocumento!.Split("_");
        var idEnte = dati[0];
        var idUtente = dati[1];
        var tipologiaDocumento = dati[2].Replace("-", " ");
        var anno = Convert.ToInt32(dati[3]);
        var hash = dati[4];
        return new DocumentiStorageKey(idEnte, idUtente, tipologiaDocumento, anno, hash);
    }

    public static string FolderPagoPA(DocumentiStorageKey linkDocumento, string parentFolder)
    {
        return $"{parentFolder}/{linkDocumento.IdUtente}/{linkDocumento.Anno}";
    }

    public static string FolderEnti(DocumentiStorageKey linkDocumento, string parentFolder)
    {
        return $"{parentFolder}/{linkDocumento.IdEnte}/{linkDocumento.IdUtente}/{linkDocumento.Anno}";
    }
    public static string FileName(DocumentiStorageKey linkDocumento)
    { 
        //linkDocumento = linkDocumento.Replace(S3Prefix, string.Empty);
        //var dati = linkDocumento!.Split("/");
        //var contractId = dati[0];
        //var partial = dati[1].Split("-");
        //var quarter = $"{partial[0]}-{partial[1]}";
        //var tipologiaDocumento = partial[2].Replace(".csv", string.Empty);
        //return new DocumentiSASStorageKey(contractId, quarter, tipologiaDocumento);
        return  string.Empty;
    }  
}