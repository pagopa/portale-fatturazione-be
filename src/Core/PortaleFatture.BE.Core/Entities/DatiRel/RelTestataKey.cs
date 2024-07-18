namespace PortaleFatture.BE.Core.Entities.DatiRel;

public sealed class RelTestataKey(string? idEnte, string? idContratto, string? tipologiaFattura, int? anno, int? mese)
{
    public string? IdEnte { get; init; } = idEnte;
    public string? IdContratto { get; init; } = idContratto;
    public string? TipologiaFattura { get; init; } = tipologiaFattura;
    public int? Anno { get; init; } = anno;
    public int? Mese { get; init; } = mese;

    public override string ToString()
    { 
        return $"{IdEnte}_{IdContratto}_{TipologiaFattura!.Replace(" ", "-")}_{Anno}_{Mese}";
    }

    public static RelTestataKey Deserialize(string idTestata)
    {
        var dati = idTestata!.Split("_");
        var anno = Convert.ToInt32(dati[3]); 
        var mese = Convert.ToInt32(dati[4]);  
        var tipologiaFattura = dati[2].Replace("-", " ");  
        var idContratto = dati[1];  
        var idEnte = dati[0];
        return new RelTestataKey(idEnte, idContratto, tipologiaFattura, anno, mese);
    }

    public static string Folder(RelTestataKey idTestata,string parentFolder)
    {
        return $"{parentFolder}/{idTestata.Anno}/{idTestata.Mese}";
    } 
    public static string FileName(
        RelTestataKey idTestata,  
        string extension = ".pdf")
    {
        return $"{idTestata}{extension}";
    }
}