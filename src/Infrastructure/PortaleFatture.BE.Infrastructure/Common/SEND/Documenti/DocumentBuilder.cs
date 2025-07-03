using DocumentFormat.OpenXml.Drawing.Diagrams;
using MimeKit.Text;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using WkHtmlToPdfDotNet;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;
public class DocumentBuilder : IDocumentBuilder
{
    private static string _fileEmailPrimoSaldo = $"primo_saldo.html";
    private static string _fileEmailSecondoSaldo = $"secondo_saldo.html";
    private static string _fileVarSemestrale = $"var_semestrale.html";
    private static string _filePrimoSaldoRel = $"PRIMO_SALDO_rel.html";
    private static string _fileSecondoSaldoRel = $"SECONDO_SALDO_rel.html";
    private static string _fileModuloCommessa = $"daticommessa.html";
    private static string _directory = $"Infrastructure/Documenti/";
    private readonly string _root;
    private readonly string _directoryPath;
    private static SynchronizedConverter _converter = new(new PdfTools());
    public DocumentBuilder(string root)
    {
        _root = root;
        _directoryPath = Path.Combine([_root, _directory]);
    }

    public string? CreateEmailHtml(RelEmail dati)
    {
        string? _fileEmail;
        if (dati.TipologiaFattura!.ToLower().Contains("primo"))
            _fileEmail = _fileEmailPrimoSaldo;
        else if (dati.TipologiaFattura!.ToLower().Contains("secondo"))
            _fileEmail = _fileEmailSecondoSaldo;
        else
            _fileEmail = _fileVarSemestrale;

        var filePath = Path.Combine([_directoryPath, _fileEmail]);
        var text = ReadFromFile(filePath);
        text = dati.Replace(text!);
        return text.Replace("\r", string.Empty).Replace("\n", string.Empty); ;
    }

    public string? CreateModuloCommessaHtml(ModuloCommessaDocumentoDto dati)
    {
        var filePath = Path.Combine([_directoryPath, _fileModuloCommessa]);
        var moduloCommessaText = ReadFromFile(filePath);
        moduloCommessaText = dati.Replace(moduloCommessaText!);
        return moduloCommessaText;
    }

    public string? CreateModuloRelHtml(RelDocumentoDto dati)
    {
        string? fileRel;
        if (dati.TipologiaFattura == TipologiaFattura.PRIMOSALDO)
        {
            fileRel = _filePrimoSaldoRel;
        }
        else if (dati.TipologiaFattura == TipologiaFattura.SECONDOSALDO)
        {
            fileRel = _fileSecondoSaldoRel;
        }
        else
        {
            throw new Exception("Tipologia fattura non valida");
        }
        var filePath = Path.Combine([_directoryPath, fileRel]);
        var relText = ReadFromFile(filePath);
        relText = dati.Replace(relText!);
        return relText;
    }

    public byte[] CreateModuloRelPdf(RelDocumentoDto dati)
    {
        string? fileRel;
        if (dati.TipologiaFattura == TipologiaFattura.PRIMOSALDO)
        {
            fileRel = _filePrimoSaldoRel;
        }
        else if (dati.TipologiaFattura == TipologiaFattura.SECONDOSALDO)
        {
            fileRel = _fileSecondoSaldoRel;
        }
        else
        {
            throw new Exception("Tipologia fattura non valida");
        }
        var filePath = Path.Combine([_directoryPath, fileRel]);
        var relText = ReadFromFile(filePath);
        relText = dati.Replace(relText!);
        return CreatePdf(relText!);
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