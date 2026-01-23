using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.pagoPA.KPIPagamenti.Extensions;
using PortaleFatture.BE.Api.Modules.pagoPA.KPIPagamenti.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.pagoPA.KPIPagamenti;

public partial class KPIPagamentiModule : Module, IRegistrableModule
{

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetKPIPagamentiMatrice(
     HttpContext context,
     [FromQuery] string year_quarter,
     [FromServices] IMediator handler,
     [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var matrice = await handler.Send(new KPIPagamentiMatriceQuery(authInfo)
        {
            YearQuarter = year_quarter,
        });

        if (matrice.IsNullNotAny())
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = matrice!.FillPagoPAOneSheet($"Matrice KPI Pagamenti {year_quarter}");
        var content = dataSet.ToExcel();

        content.Seek(0, SeekOrigin.Begin);
        return Results.Stream(content!, mime, filename);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<GridKPIPagamentiScontoReportListDto>, NotFound>> PostKPIPagamenti(
     HttpContext context,
     [FromBody] KPIPagamentiRequest request,
     [FromServices] IMediator handler,
     [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var kpiPagamenti = await handler.Send(new KPIPagamentiScontoQuery(authInfo)
        {
            MembershipId = request.MembershipId,
            ProviderName = request.ProviderName,
            Quarters = request.Quarters,
            RecipientIds = request.ContractIds,
            Year = request.Year,
            RecipientId = request.RecipientId,  
        });

        if (kpiPagamenti.KPIPagamentiScontoReports.IsNullNotAny())
            return NotFound();

        return Ok(kpiPagamenti);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostKPIPagamentiDocument(
    HttpContext context,
    [FromBody] KPIPagamentiRequest request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var kpiPagamenti = await handler.Send(new KPIPagamentiScontoQuery(authInfo)
        {
            MembershipId = request.MembershipId,
            ProviderName = request.ProviderName,
            Quarters = request.Quarters,
            RecipientIds = request.ContractIds,
            Year = request.Year,
            RecipientId = request.RecipientId,
        });

        if (kpiPagamenti.KPIPagamentiScontoReports.IsNullNotAny())
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var scontoExcel = kpiPagamenti.KPIPagamentiScontoReports!.Map();
        var dataSet = scontoExcel!.FillPagoPAOneSheet("KPI Pagamenti");
        var content = dataSet.ToExcel();

        content.Seek(0, SeekOrigin.Begin);
        return Results.Stream(content!, mime, filename);
    }
}