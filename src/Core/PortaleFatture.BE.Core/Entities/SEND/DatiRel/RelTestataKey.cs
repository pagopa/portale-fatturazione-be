namespace PortaleFatture.BE.Core.Entities.SEND.DatiRel;

/// <summary>
/// Rappresenta una chiave composta per identificare in modo univoco una testata, includendo l'ID dell'ente, l'ID del
/// contratto, la tipologia di fattura, l'anno e il mese.
/// </summary>
/// <param name="idEnte">L'identificativo dell'ente associato alla testata. Può essere null se non specificato.</param>
/// <param name="idContratto">L'identificativo del contratto relativo alla testata. Può essere null se non specificato.</param>
/// <param name="tipologiaFattura">La tipologia della fattura associata alla testata. Può essere null se non specificato.</param>
/// <param name="anno">L'anno di riferimento della testata. Può essere null se non specificato.</param>
/// <param name="mese">Il mese di riferimento della testata. Può essere null se non specificato.</param>
public sealed class RelTestataKey(string? idEnte, string? idContratto, string? tipologiaFattura, int? anno, int? mese)
{
    public string? IdEnte { get; init; } = idEnte;
    public string? IdContratto { get; init; } = idContratto;
    public string? TipologiaFattura { get; init; } = tipologiaFattura;
    public int? Anno { get; init; } = anno;
    public int? Mese { get; init; } = mese;

    /// <summary>
    /// Restituisce una stringa che rappresenta l'oggetto corrente, includendo l'ID dell'ente, l'ID del contratto,
    /// la tipologia di fattura, l'anno e il mese.
    /// </summary>
    /// <returns>Una stringa nel formato "{IdEnte}_{IdContratto}_{TipologiaFattura}_{Anno}_{Mese}", dove gli spazi
    /// nella tipologia di fattura sono sostituiti con trattini.</returns>
    public override string ToString()
    {
        return $"{IdEnte}_{IdContratto}_{TipologiaFattura!.Replace(" ", "-")}_{Anno}_{Mese}";
    }

    /// <summary>
    /// Deserializza una stringa identificativa in un'istanza di RelTestataKey.
    /// </summary>
    /// <remarks>La stringa di input deve seguire il formato previsto, altrimenti potrebbero verificarsi
    /// eccezioni di runtime durante la conversione o l'accesso agli elementi.</remarks>
    /// <param name="idTestata">La stringa identificativa da deserializzare. Deve essere composta da segmenti separati da caratteri di
    /// sottolineatura ('_') e contenere almeno cinque elementi nei formati attesi.</param>
    /// <returns>Un oggetto RelTestataKey che rappresenta i dati estratti dalla stringa identificativa.</returns>
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

    /// <summary>
    /// Genera il percorso di una cartella basato sull'anno e il mese specificati dalla chiave della testata.
    /// </summary>
    /// <param name="idTestata">La chiave della testata che fornisce i valori di anno e mese da includere nel percorso della cartella.</param>
    /// <param name="parentFolder">Il percorso della cartella principale a cui aggiungere i sottopercorsi di anno e mese. Non può essere null o
    /// vuoto.</param>
    /// <returns>Una stringa che rappresenta il percorso della cartella risultante, composta dal percorso principale seguito da
    /// anno e mese.</returns>
    public static string Folder(RelTestataKey idTestata, string parentFolder)
    {
        return $"{parentFolder}/{idTestata.Anno}/{idTestata.Mese}";
    }

    /// <summary>
    /// Genera il nome di un file combinando la chiave della testata e l'estensione specificata.
    /// </summary>
    /// <param name="idTestata">La chiave identificativa della testata da includere nel nome del file.</param>
    /// <param name="extension">L'estensione del file, inclusivo del punto iniziale. Il valore predefinito è ".pdf".</param>
    /// <returns>Una stringa che rappresenta il nome del file risultante dalla concatenazione di 'idTestata' e 'extension'.</returns>
    public static string FileName(
        RelTestataKey idTestata,
        string extension = ".pdf")
    {
        return $"{idTestata}{extension}";
    }
}