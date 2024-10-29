using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.pagoPA.AnagraficaPSP.Extensions;
using PortaleFatture.BE.Api.Modules.pagoPA.AnagraficaPSP.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.pagoPA.Auth;


public partial class PSPModule : Module, IRegistrableModule
{

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<PSPListDto>, NotFound>> PostPSPList(
    HttpContext context,
    [FromBody] PSPRequest request,
    [FromServices] IMediator handler,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var psp = await handler.Send(request.Map(authInfo, page, pageSize));
        if (psp == null || psp.Count == 0)
            return NotFound();
        return Ok(psp);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ContractIdPSP>>, NotFound>> PostPSPListByName(
    HttpContext context,
    [FromBody] PSPRequestName request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var psp = await handler.Send(request.Map(authInfo));
        if (psp == null || psp.Count() == 0)
            return NotFound();
        return Ok(psp);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostPSPDocumentListByName(
    HttpContext context,
    [FromBody] PSPRequest request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer)
    { 
        var authInfo = context.GetAuthInfo();
        var psp = await handler.Send(request.Map(authInfo));
        if (psp == null || psp.Count == 0)
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = psp.PSPs!.FillPagoPAOneSheet("Anagrafica PSP");
        var content = dataSet.ToExcel();

        content.Seek(0, SeekOrigin.Begin);
        return Results.Stream(content!, mime, filename);
    }
}