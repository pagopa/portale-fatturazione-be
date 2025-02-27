﻿using PortaleFatture.BE.Api.Modules.SEND.Accertamenti.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Messaggi;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries;

namespace PortaleFatture.BE.Api.Modules.SEND.Accertamenti.Extensions;

public static class AccertamentiExtensions
{
    public static ReportQueryGetByRicerca Map(this AccertamentiReportRicercaRequest request, AuthenticationInfo? authInfo)
    {
        return new ReportQueryGetByRicerca(authInfo!)
        {
            Anno = request.Anno,
            Mese = request.Mese,
            CategoriaDocumento = TipologiaDocumento.Accertamenti
        };
    }

    public static ReportQueryGetById Map(this AccertamentoReportByIdRequest request, AuthenticationInfo? authInfo)
    {
        return new ReportQueryGetById(authInfo!)
        {
            IdReport = request.IdReport
        };
    }

    public static MessaggioCreateCommand Mapv2(this ReportDto report, AuthenticationInfo authInfo, string? contentType, string? contentLanguage)
    {
        return new MessaggioCreateCommand(authInfo)
        {
            Anno = report.Anno,
            Mese = report.Mese,
            Json = report.Json,
            TipologiaDocumento = report.Descrizione,
            ContentType = contentType,
            ContentLanguage = contentLanguage,
            LinkDocumento = report.LinkDocumento,
            Stato = TipologiaStatoMessaggio.Completato,
            IdReport = report.IdReport,
            Hash = report.Hash,
            CategoriaDocumento = report.CategoriaDocumento
        };
    }
}