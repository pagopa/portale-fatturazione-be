using System.Runtime.CompilerServices;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Response;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Services;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche.Extensions;

public static class ContestazioniExtensions
{
    private static ReportContestazioneStepsWithLinkDto Map(this ReportContestazioneStepsDto source, string? linkDocumento)
    {
        return new ReportContestazioneStepsWithLinkDto
        {
            ReportId = source.ReportId,
            Step = source.Step,
            DescrizioneStep = source.DescrizioneStep,
            TotaleNotificheAnalogicheARInternazionaliRIR = source.TotaleNotificheAnalogicheARInternazionaliRIR,
            TotaleNotificheAnalogicheARNazionaliAR = source.TotaleNotificheAnalogicheARNazionaliAR,
            TotaleNotificheAnalogicheRSInternazionaliRIS = source.TotaleNotificheAnalogicheRSInternazionaliRIS,
            TotaleNotificheAnalogicheRSNazionaliRS = source.TotaleNotificheAnalogicheRSNazionaliRS,
            TotaleNotificheAnalogiche890 = source.TotaleNotificheAnalogiche890,
            TotaleNotificheDigitali = source.TotaleNotificheDigitali,
            TotaleNotifiche = source.TotaleNotifiche,
            Link = source.Link,
            NonContestataAnnullata = source.NonContestataAnnullata,
            ContestataEnte = source.ContestataEnte,
            RispostaEnte = source.RispostaEnte,
            Accettata = source.Accettata,
            RispostaSend = source.RispostaSend,
            RispostaRecapitista = source.RispostaRecapitista,
            RispostaConsolidatore = source.RispostaConsolidatore,
            Rifiutata = source.Rifiutata,
            NonFatturabile = source.NonFatturabile,
            Fatturabile = source.Fatturabile,
            Storage = source.Storage,
            NomeDocumento = source.NomeDocumento,
            DataCompletamento = source.DataCompletamento,
            LinkDocumento = linkDocumento
        };
    }

    public static ContestazioneReportReponse? Map(this IEnumerable<ReportContestazioneStepsDto> steps, IContestazioniStorageService storageService)
    {
        var report = new ContestazioneReportReponse()
        {
            Steps = [],
        };

        foreach (var s in steps)
        {
            var linkDocumento = string.Empty;
            if (!string.IsNullOrEmpty(s.NomeDocumento!))
                linkDocumento = storageService.GetSASToken(s.Link!, s.NomeDocumento!);

            var model = s.Map(linkDocumento);
            report.Steps.Add(model);
        }
        return report;
    }

    public static List<ContestazioniMeseResponse> Map(this IEnumerable<string> mesi)
    {
        var cMesi = new List<ContestazioniMeseResponse>();
        foreach (var mese in mesi)
        {
            cMesi.Add(new ContestazioniMeseResponse()
            {
                Mese = mese,
                Descrizione = Convert.ToInt32(mese).GetMonth()
            });
        }
        return cMesi;
    }

    private static string GetContentTypeFromBytes(this byte[] fileBytes, string? fileName = null)
    {
        // If file name is provided, check extension first
        if (!string.IsNullOrEmpty(fileName))
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (extension == ".csv")
            {
                return "text/csv"; // Return CSV type
            }
        }

        // Check for JPEG (starts with FF D8 FF)
        if (fileBytes.Length >= 3 && fileBytes[0] == 0xFF && fileBytes[1] == 0xD8 && fileBytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        // Check for PNG (starts with 89 50 4E 47)
        if (fileBytes.Length >= 4 && fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47)
        {
            return "image/png";
        }

        // Check for GIF (starts with 47 49 46 38)
        if (fileBytes.Length >= 4 && fileBytes[0] == 0x47 && fileBytes[1] == 0x49 && fileBytes[2] == 0x46 && fileBytes[3] == 0x38)
        {
            return "image/gif";
        }

        // Check for PDF (starts with %PDF)
        if (fileBytes.Length >= 5 && fileBytes[0] == 0x25 && fileBytes[1] == 0x50 && fileBytes[2] == 0x44 && fileBytes[3] == 0x46)
        {
            return "application/pdf";
        }

        // Check for CSV (check if file contains commas or lines like CSV)
        if (fileBytes.Length >= 5)
        {
            string text = System.Text.Encoding.UTF8.GetString(fileBytes, 0, Math.Min(fileBytes.Length, 1024)); // Read first 1KB

            // Check if the file contains commas and new lines, typical of CSV
            if (text.Contains(',') && text.Contains('\n'))
            {
                return "text/csv";
            }
        }

        // Default content type
        return "application/octet-stream";

    }
    private static IFormFile ConvertToFormFile(this byte[] fileBytes, string fileName, string contentType)
    {
        var stream = new MemoryStream(fileBytes);
        var formFile = new FormFile(stream, 0, fileBytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
        return formFile;
    }

    public static async Task<(List<IFormFile> Chunks, int TotalChunks)> DivideFileIntoChunks(this IFormFile file, int chunkSize)
    {
        var fileName = file.FileName;
        var chunks = new List<IFormFile>();
        var totalChunks = 0;

        using (var stream = file.OpenReadStream())
        {
            var buffer = new byte[chunkSize];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                if (bytesRead < chunkSize)
                {
                    var lastChunk = new byte[bytesRead];
                    Array.Copy(buffer, lastChunk, bytesRead);
                    var contentType = lastChunk.GetContentTypeFromBytes(fileName);
                    chunks.Add(lastChunk.ConvertToFormFile(fileName, contentType));
                }
                else
                {
                    var clone = (byte[])buffer.Clone();
                    var contentType = clone.GetContentTypeFromBytes(fileName);
                    chunks.Add(clone.ConvertToFormFile(fileName, contentType));
                }

                totalChunks++;
            }
        }

        return (chunks, totalChunks);
    }

    public static UploadContestazioni Map(this UploadContestazioniEnte upload, string idEnte, string idContratto)
        => new()
        {
            FileChunk = upload.FileChunk, 
            FileId = upload.FileId,
            TotalChunks = upload.TotalChunks,
            ChunkIndex = upload.ChunkIndex,
            Anno = upload.Anno,
            Mese = upload.Mese,
            IdEnte = idEnte,
            ContractId = idContratto
        };

    public static async Task<UploadContestazioni> MapAndValidate(this HttpContext context, UploadContestazioni upload, IStringLocalizer<Localization> localizer)
    {
        var request = context.Request;
        if (!request.HasFormContentType)
            throw new UploadException(localizer["ContestazioniFormEmpty"]);

        var form = await request.ReadFormAsync();
        if (form.IsNullNotAny() || form.Files.IsNullNotAny())
            throw new UploadException(localizer["ContestazioniFormEmpty"]);

        var chunk = upload.FileChunk;
        var fileName = chunk!.FileName;

        if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            throw new UploadException(localizer["ContestazioniOnlyCSV"]);

        if (upload.ChunkIndex == 0)
        {
            using var reader = new StreamReader(chunk!.OpenReadStream());
            var headerLine = await reader.ReadLineAsync() ??
                  throw new UploadException(localizer["ContestazioniOnlyCSV"]);

            var firstDataRow = await reader.ReadLineAsync() ??
                  throw new UploadException(localizer["ContestazioniOnlyCSV"]);

            var columns = firstDataRow.Split(';');
            if (upload.ContractId != columns[0])
                throw new UploadException(localizer["ContestazioniContractEmpty", upload.ContractId!, columns[0]]);

            if (upload.IdEnte != columns[16])
                throw new UploadException(localizer["ContestazioniEnteEmpty", upload.IdEnte!, columns[16]]);
        }
        //upload.FileName = fileName;
        return upload;
    }
}