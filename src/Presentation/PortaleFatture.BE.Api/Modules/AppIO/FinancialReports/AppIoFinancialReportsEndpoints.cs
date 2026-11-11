using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports;

/// <summary>
/// Registrazione delle rotte dei documenti emessi APP IO (PF-908, modello dati aggiornato con PF-1016).
/// </summary>
public partial class AppIoFinancialReportsModule
{
    /// <summary>
    /// Registra le rotte api/appio/financialreports*: filtri Anno, Trimestre e Codice Fiscale, griglia,
    /// download "Documenti Contabili", dettaglio di un documento e suo download. Il filtro "Nome Ente"
    /// è api/appio/contracts/name.
    /// </summary>
    /// <param name="endpointRouteBuilder">Il builder delle rotte dell'applicazione.</param>
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder
           .MapGet("api/appio/financialreports/years", GetAppIoFinancialReportsYears)
           .WithName("Permette di visualizzare gli anni in cui sono presenti documenti contabili AppIO.")
           .SetOpenApi(Module.DatiFattureLabelAppIO)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/appio/financialreports/quarters", PostAppIoFinancialReportsQuarters)
           .WithName("Permette di visualizzare i trimestri dei documenti contabili AppIO per anno.")
           .SetOpenApi(Module.DatiFattureLabelAppIO)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/appio/financialreports/codfiscale", GetAppIoFinancialReportsCodiciFiscali)
           .WithName("Permette di visualizzare i codici fiscali degli enti AppIO.")
           .SetOpenApi(Module.DatiFattureLabelAppIO)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/appio/financialreports", PostAppIoFinancialReports)
           .WithName("Permette di visualizzare i documenti contabili AppIO.")
           .SetOpenApi(Module.DatiFattureLabelAppIO)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/appio/financialreports/document", PostAppIoFinancialReportsDocument)
           .WithName("Permette di scaricare il file excel dei documenti contabili AppIO.")
           .SetOpenApi(Module.DatiFattureLabelAppIO)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/appio/financialreports/detail", PostAppIoFinancialReportsDetail)
           .WithName("Permette di visualizzare il dettaglio di un documento contabile AppIO.")
           .SetOpenApi(Module.DatiFattureLabelAppIO)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/appio/financialreports/detail/download", PostAppIoFinancialReportsDetailDownload)
           .WithName("Permette di scaricare il dettaglio di un documento contabile AppIO.")
           .SetOpenApi(Module.DatiFattureLabelAppIO)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
}
