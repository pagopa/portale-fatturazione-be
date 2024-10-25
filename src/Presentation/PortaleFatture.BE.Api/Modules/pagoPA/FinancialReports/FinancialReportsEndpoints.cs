using Microsoft.AspNetCore.Cors;
using PortaleFatture.BE.Api.Infrastructure;

namespace PortaleFatture.BE.Api.Modules.pagoPA.FinancialReports;

public partial class FinancialReportsModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {  
        endpointRouteBuilder
           .MapPost("api/v2/pagopa/financialreports", PostFinancialReportsList)
           .WithName("Permette di visualizzare i dati relativi ai financial reports pagoPA")
           .SetOpenApi(Module.FinancialReports)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapPost("api/v2/pagopa/financialreports/dettaglio", PostFinancialReportsByPSP)
           .WithName("Permette di visualizzare i dati relativi ai financial reports PSP pagoPA")
           .SetOpenApi(Module.FinancialReports)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
 
        endpointRouteBuilder
           .MapPost("api/v2/pagopa/financialreports/quarters", PostFinancialReportsQuarters)
           .WithName("Permette di visualizzare i quarter per year")
           .SetOpenApi(Module.FinancialReports)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
           .MapGet("api/v2/pagopa/financialreports/years", GetFinancialReportsYears)
           .WithName("Permette di visualizzare tutti gli anni dove ho dei financial records")
           .SetOpenApi(Module.FinancialReports)
           .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/v2/pagopa/financialreports/document", PostFinancialReportsDocument)
          .WithName("Permette di visualizzare i dati relativi ai financial reports pagoPA via excel")
          .SetOpenApi(Module.FinancialReports)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));

        endpointRouteBuilder
          .MapPost("api/v2/pagopa/financialreports/documentpdnd", PostFinancialReportsPdndDocument)
          .WithName("Permette di visualizzare i dati relativi ai financial reports Pdnd pagoPA via excel")
          .SetOpenApi(Module.FinancialReports)
          .WithMetadata(new EnableCorsAttribute(policyName: Module.CORSLabel));
    }
} 