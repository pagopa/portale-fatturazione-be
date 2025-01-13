using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti;

public class DocumentPspBuilder
{
    private static string _apiKeyFile = $"credentials.json";
    private static string _fileEmailFinancial = $"email_psp.html";
    private static string _directory = $"Infrastructure/Documenti/pagoPA/";
    private readonly string _root;
    private readonly string _directoryPath;

    public DocumentPspBuilder(string root)
    {
        _root = root;
        _directoryPath = Path.Combine([_root, _directory]);
    }

    public string? CreateEmailHtml(PspEmail dati)
    {
        var filePath = Path.Combine([_directoryPath, _fileEmailFinancial]);
        var text = ReadFromFile(filePath);
        text = Replace(dati, text!);
        return text;
    }
    public string? ApiKeyFilePath()
    {
        return Path.Combine([_directoryPath, _apiKeyFile]);
    }

    private string? ReadFromFile(string filePath)
    {
        if (File.Exists(filePath))
            return File.ReadAllText(filePath);

        var msg = "Add a template in the document folder!";
        throw new ConfigurationException(msg);
    }

    private string Replace(PspEmail model, string template)
    {
        var htmlString = @"<tr style=""display:flex"">
                      <td style=""width:20%; padding: 4px 20px 0 0;"">Discount Quarter Report</td>
                      <td style=""padding: 4px 0;""><a href=""[DiscountReport]"" style=""color: #00B2C2; text-decoration: none;"">Download Discount Quarter Report</a></td>
                  </tr>";

        if (!string.IsNullOrEmpty(model.DiscountReport)) 
            model.DiscountReport = htmlString.Replace(nameof(model.DiscountReport).GetName<PspEmail>(), model.DiscountReport); 

        template = template
            .Replace(nameof(model.Anno).GetName<PspEmail>(), model.Anno.ToString())
            .Replace(nameof(model.Trimestre).GetName<PspEmail>(), model.Trimestre)
            .Replace(nameof(model.IdContratto).GetName<PspEmail>(), model.IdContratto)
            .Replace(nameof(model.Tipologia).GetName<PspEmail>(), model.Tipologia)
            .Replace(nameof(model.Email).GetName<PspEmail>(), model.Email)
            .Replace(nameof(model.RagioneSociale).GetName<PspEmail>(), model.RagioneSociale)
            .Replace(nameof(model.DetailReport).GetName<PspEmail>(), model.DetailReport)
            .Replace(nameof(model.AgentReport).GetName<PspEmail>(), model.AgentReport)
            .Replace(nameof(model.DiscountReport).GetName<PspEmail>(), model.DiscountReport)
            ;

        return template;
    }
}