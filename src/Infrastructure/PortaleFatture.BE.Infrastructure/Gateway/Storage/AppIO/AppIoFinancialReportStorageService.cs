using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using PortaleFatture.BE.Core.Common;

namespace PortaleFatture.BE.Infrastructure.Gateway.Storage.AppIO;

/// <summary>
/// Lettura dal blob storage dei file dei financial report APP IO, indicati da <c>file_path</c> di
/// [be].[vwAppioFinalReport].
/// </summary>
public interface IAppIoFinancialReportStorageService
{
    /// <summary>
    /// Dice se il file indicato è leggibile con la configurazione corrente: storage configurato
    /// (account e chiave), account del percorso uguale a quello configurato, container noto.
    /// </summary>
    /// <param name="filePath">Il valore di file_path.</param>
    /// <returns>true se il download può essere tentato.</returns>
    bool CanRead(string filePath);

    /// <summary>
    /// Apre in lettura il file indicato.
    /// </summary>
    /// <param name="filePath">Il valore di file_path.</param>
    /// <param name="cancellationToken">Il token di annullamento.</param>
    /// <returns>Il contenuto del file, oppure null se il blob non esiste.</returns>
    Task<Stream?> OpenReadAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Posizione di un file sul blob storage, ricavata da <c>file_path</c>.
/// </summary>
/// <param name="Account">Lo storage account, se il percorso lo indica.</param>
/// <param name="Container">Il container (file system del data lake), se il percorso lo indica.</param>
/// <param name="BlobName">Il percorso del file dentro il container.</param>
public sealed record AppIoFilePath(string? Account, string? Container, string BlobName)
{
    /// <summary>
    /// Interpreta <c>file_path</c>. Tre forme:
    /// <list type="bullet">
    /// <item><c>abfss://container@account.dfs.core.windows.net/percorso</c> — quella scritta da DATA;</item>
    /// <item><c>https://account.blob.core.windows.net/container/percorso</c>;</item>
    /// <item><c>percorso</c> relativo: account e container vengono dalla configurazione.</item>
    /// </list>
    /// </summary>
    /// <param name="filePath">Il valore di file_path.</param>
    /// <returns>La posizione del file.</returns>
    public static AppIoFilePath Parse(string filePath)
    {
        var path = filePath.Trim();
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme is "abfss" or "abfs")
                return new(uri.Host.Split('.')[0], Uri.UnescapeDataString(uri.UserInfo), Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/'));
            if (uri.Scheme == Uri.UriSchemeHttps)
            {
                var blob = new BlobUriBuilder(uri);
                return new(blob.AccountName, blob.BlobContainerName, blob.BlobName);
            }
        }
        return new(null, null, path.TrimStart('/'));
    }
}

/// <inheritdoc cref="IAppIoFinancialReportStorageService"/>
/// <param name="options">Le opzioni dell'applicazione (sezione <c>StorageAppIO</c>).</param>
public sealed class AppIoFinancialReportStorageService(IPortaleFattureOptions options) : IAppIoFinancialReportStorageService
{
    private readonly StorageAppIO? _storage = options.StorageAppIO;

    /// <summary>
    /// L'URL del servizio blob: <see cref="StorageAppIO.Endpoint"/> se valorizzato (con o senza schema,
    /// es. un private endpoint o un DNS personalizzato), altrimenti
    /// <c>https://&lt;AccountName&gt;.blob.core.windows.net</c>. Anche un data lake (abfss, endpoint dfs)
    /// espone gli stessi file dall'endpoint blob.
    /// </summary>
    /// <param name="storage">La configurazione dello storage.</param>
    /// <returns>L'URL, oppure null se l'endpoint configurato non è un URL https valido.</returns>
    public static Uri? BlobServiceUri(StorageAppIO storage)
    {
        var endpoint = storage.Endpoint?.Trim();
        if (string.IsNullOrEmpty(endpoint))
            return new Uri($"https://{storage.AccountName}.blob.core.windows.net");

        if (!endpoint.Contains("://"))
            endpoint = "https://" + endpoint;
        return Uri.TryCreate(endpoint.TrimEnd('/'), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri
            : null;
    }

    /// <inheritdoc/>
    public bool CanRead(string filePath)
    {
        if (string.IsNullOrWhiteSpace(_storage?.AccountName) || string.IsNullOrWhiteSpace(_storage.AccountKey))
            return false;
        // Un endpoint configurato ma non valido equivale a storage non configurato (503), non a un 500
        if (BlobServiceUri(_storage) is null)
            return false;

        var file = AppIoFilePath.Parse(filePath);
        // La chiave configurata vale per un solo account: un percorso che ne indica un altro non è leggibile
        if (file.Account != null && !file.Account.Equals(_storage.AccountName, StringComparison.OrdinalIgnoreCase))
            return false;
        return !string.IsNullOrWhiteSpace(file.Container ?? _storage.BlobContainerName) && file.BlobName.Length > 0;
    }

    /// <inheritdoc/>
    public async Task<Stream?> OpenReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var file = AppIoFilePath.Parse(filePath);
        var credential = new StorageSharedKeyCredential(_storage!.AccountName, _storage.AccountKey);
        var service = new BlobServiceClient(BlobServiceUri(_storage)!, credential);
        var blob = service.GetBlobContainerClient(file.Container ?? _storage.BlobContainerName).GetBlobClient(file.BlobName);
        try
        {
            return await blob.OpenReadAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
