using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Report.Commands;
using PortaleFatture.BE.Infrastructure.Common.Report.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.Report.Extensions;

public static class ReportExtensions
{
    public static ReportDto Map(this ReportCreateCommand command, long idReport)
    {
        return new ReportDto()
        {
            Anno = command.Anno,
            Mese = command.Mese,
            ContentLanguage = command.ContentLanguage,
            ContentType = command.ContentType,
            DataInserimento = DateTime.UtcNow.ItalianTime(),
            Descrizione = command.Descrizione,
            Hash = command.Hash,
            IdReport = idReport,
            Json = command.Json,
            LinkDocumento = command.LinkDocumento,
            Prodotto = command.Prodotto,
            Stato = command.Stato,
            TipologiaDocumento = command.TipologiaDocumento
        };
    }
}