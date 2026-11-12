using System.Text;
using PortaleFatture.BE.Infrastructure.Gateway.Storage.AppIO;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Sostituto di <see cref="IAppIoFinancialReportStorageService"/> nei test: il blob storage non si
/// raggiunge dal container e nessun test deve chiamare Azure. Restituisce un contenuto riconoscibile,
/// registra i percorsi richiesti (così i test verificano che al servizio arrivi il file_path del
/// trimestre) e permette di simulare lo storage non configurato e il file assente.
/// </summary>
public sealed class FakeAppIoFinancialReportStorageService : IAppIoFinancialReportStorageService
{
    public static readonly FakeAppIoFinancialReportStorageService Instance = new();

    /// <summary>Contenuto restituito per ogni file presente.</summary>
    public const string Contenuto = "contenuto;del;financial;report\n1;2;3;4\n";

    private readonly object _lock = new();
    private readonly List<string> _richieste = [];

    /// <summary>false simula lo storage non configurato per il percorso (la rotta risponde 503).</summary>
    public bool Leggibile { get; set; } = true;

    /// <summary>true simula il blob assente (la rotta risponde 404).</summary>
    public bool Assente { get; set; }

    /// <summary>I percorsi di cui è stata chiesta l'apertura, in ordine.</summary>
    public IReadOnlyList<string> Richieste
    {
        get { lock (_lock) return _richieste.ToList(); }
    }

    public void Reset()
    {
        lock (_lock)
        {
            Leggibile = true;
            Assente = false;
            _richieste.Clear();
        }
    }

    public bool CanRead(string filePath) => Leggibile;

    public Task<Stream?> OpenReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        lock (_lock) _richieste.Add(filePath);
        return Task.FromResult<Stream?>(Assente ? null : new MemoryStream(Encoding.UTF8.GetBytes(Contenuto)));
    }
}
