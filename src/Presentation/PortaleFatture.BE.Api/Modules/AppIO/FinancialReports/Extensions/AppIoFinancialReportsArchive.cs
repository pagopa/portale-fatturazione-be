using System.IO.Compression;
using PortaleFatture.BE.Infrastructure.Gateway.Storage.AppIO;

namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Extensions;

/// <summary>
/// Download dei file dei financial report APP IO (api/appio/financialreports/blob/download): se c'è un
/// solo file, il file; se ce ne sono più, uno ZIP con un file per trimestre.
/// </summary>
public static class AppIoFinancialReportsArchive
{
    /// <summary>
    /// Costruisce la risposta del download.
    /// <list type="bullet">
    /// <item>404 se nessun trimestre chiesto ha un file (né in vista né sul blob storage);</item>
    /// <item>503 se lo storage non è configurato per anche uno solo dei percorsi;</item>
    /// <item>un solo file trovato: il file; più file: uno ZIP, uno per trimestre. Conta il numero di file
    /// effettivamente disponibili, non quello dei trimestri chiesti: i trimestri senza file si saltano.</item>
    /// </list>
    /// </summary>
    /// <param name="trimestri">I trimestri chiesti, validati e in ordine crescente.</param>
    /// <param name="percorsi">Trimestre → file_path, solo per i trimestri con documenti.</param>
    /// <param name="storage">Il servizio di lettura dal blob storage.</param>
    /// <param name="ct">Il token di annullamento.</param>
    /// <returns>La risposta HTTP.</returns>
    public static async Task<IResult> ToDownload(
        this IReadOnlyList<string> trimestri,
        IReadOnlyDictionary<string, string> percorsi,
        IAppIoFinancialReportStorageService storage,
        CancellationToken ct)
    {
        var candidati = trimestri.Where(percorsi.ContainsKey).Select(t => (Trimestre: t, Percorso: percorsi[t])).ToList();
        if (candidati.Count == 0)
            return Results.NotFound();
        if (candidati.Any(f => !storage.CanRead(f.Percorso)))
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

        // Si aprono tutti prima di scegliere la forma: un file assente sul blob non deve trasformare
        // in ZIP (con un solo file dentro) quello che è un download singolo. I trimestri sono al più quattro.
        var aperti = new List<(string Trimestre, string Percorso, Stream Contenuto)>();
        try
        {
            foreach (var (trimestre, percorso) in candidati)
            {
                var contenuto = await storage.OpenReadAsync(percorso, ct);
                if (contenuto is not null)
                    aperti.Add((trimestre, percorso, contenuto));
            }

            if (aperti.Count == 0)
                return Results.NotFound();
            if (aperti.Count == 1)
            {
                var (trimestre, percorso, contenuto) = aperti[0];
                aperti.Clear(); // lo stream passa alla risposta, che lo chiude
                return Results.Stream(contenuto, percorso.MimeType(), AppIoFinancialReportsExtensions.FileName(trimestre, percorso));
            }

            var zip = await CreaZip(aperti, ct);
            return Results.Stream(zip, "application/zip",
                AppIoFinancialReportsExtensions.ZipName([.. aperti.Select(a => a.Trimestre)]));
        }
        finally
        {
            foreach (var (_, _, contenuto) in aperti)
                await contenuto.DisposeAsync();
        }
    }

    // Lo ZIP si scrive su un file temporaneo (eliminato alla chiusura dello stream) e non direttamente
    // sulla risposta: ZipArchive scrive in modo sincrono alla Dispose, che Kestrel non consente, e non
    // si tiene in memoria un archivio di dimensione non nota.
    private static async Task<Stream> CreaZip(IReadOnlyList<(string Trimestre, string Percorso, Stream Contenuto)> file, CancellationToken ct)
    {
        var temp = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, 81920,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        try
        {
            using (var archivio = new ZipArchive(temp, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var (trimestre, percorso, contenuto) in file)
                {
                    var voce = archivio.CreateEntry(AppIoFinancialReportsExtensions.FileName(trimestre, percorso), CompressionLevel.Optimal);
                    await using var destinazione = voce.Open();
                    await contenuto.CopyToAsync(destinazione, ct);
                }
            }
            temp.Position = 0;
            return temp;
        }
        catch
        {
            await temp.DisposeAsync();
            throw;
        }
    }
}
