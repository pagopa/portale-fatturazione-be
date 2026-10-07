using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports;

/// <summary>
/// Registrazione delle rotte dei documenti contabili APP IO (PF-908).
/// </summary>
public partial class AppIoFinancialReportsModule
{
    /// <summary>
    /// Registra le rotte api/appio/financialreports*: filtri Anno e Trimestre, griglia e i due
    /// download Excel ("Documenti Contabili" e "Financial Report").
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
           .MapPost("api/appio/financialreports/detail/download", PostAppIoFinancialReportsDetailDownload)
           .WithName("Permette di scaricare il financial report AppIO.")
           .SetOpenApi(Module.DatiFattureLabelAppIO)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
}
