using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Exceptions;
using WkHtmlToPdfDotNet;

namespace PortaleFatture.BE.Infrastructure.Common.Documenti;
public class DocumentBuilder : IDocumentBuilder
{
    private static string _fileModuloCommessa = $"daticommessa.html";
    private static string _directory = $"Infrastructure/Documenti/";
    private readonly string _root;
    private readonly string _directoryPath;
    private static SynchronizedConverter _converter = new(new PdfTools());
    public DocumentBuilder(string root)
    {
        this._root = root;
        this._directoryPath = Path.Combine([_root, _directory]);
    }

    public byte[] CreateModuloCommessaPdf(ModuloCommessaDocumentoDto dati)
    {
        var filePath = Path.Combine([_directoryPath, _fileModuloCommessa]);
        var moduloCommessaText = ReadFromFile(filePath);
        moduloCommessaText = dati.Replace(moduloCommessaText!);
        return CreatePdf(moduloCommessaText!);
    }

    private readonly static GlobalSettings _globalSettings = new()
    {
        ColorMode = ColorMode.Color,
        Orientation = Orientation.Portrait,
        PaperSize = PaperKind.A4
    };

    private byte[] CreatePdf(string text)
    {
        var doc = new HtmlToPdfDocument()
        {
            GlobalSettings = _globalSettings,
            Objects = {
                new ObjectSettings() {
                    PagesCount = true,
                    HtmlContent = text,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
        };
        return _converter.Convert(doc);
    }

    private string? ReadFromFile(string filePath)
    {
        if (File.Exists(filePath))
            return File.ReadAllText(filePath);

        var msg = "Add a pdf template in the document folder!";
        throw new ConfigurationException(msg);
    }

    private string? ReadFromFile(string root, string directory, string fileName)
    {
        string[] paths = [root, directory, fileName];
        var filePath = Path.Combine(paths);
        return ReadFromFile(filePath);
    }
}