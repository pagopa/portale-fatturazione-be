using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.SEND.Orchestratore.Payload;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.SEND.Orchestratore;

public partial class OrchestratoreModule
{
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<OrchestratoreDto>, NotFound>> PostListOrchestratoreAsync(
    HttpContext context,
    [FromBody] OrchestratoreByDateRequest req,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var orchestratore = await handler.Send(new OrchestratoreByDateQuery(authInfo)
        {
            Init = req.Init,
            End = req.End,
            Stati = req.Stati,
            Page = page,
            Size = pageSize,
        });
        if (orchestratore == null || orchestratore.Items.IsNullNotAny())
            return NotFound();

        return Ok(orchestratore);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private Results<Ok<Dictionary<int, string>>, NotFound> GetOrchestratoreStati(
      HttpContext context,
      [FromServices] IStringLocalizer<Localization> localizer,
      [FromServices] IMediator handler)
    {
        return Ok(StatiQuery.GetStati());
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostDownloadListOrchestratoreAsync(
       HttpContext context,
       [FromBody] OrchestratoreByDateRequest req, 
       [FromServices] IStringLocalizer<Localization> localizer,
       [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var orchestratore = await handler.Send(new OrchestratoreByDateQuery(authInfo)
        {
            Init = req.Init,
            End = req.End,
            Stati = req.Stati,
            Page = null,
            Size = null,
        });

        if (orchestratore == null || orchestratore.Items.IsNullNotAny())
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = orchestratore.Items!.FillOneSheetv2();
        var stream = dataSet.ToExcel();

        stream.Position = 0;
        return Results.Stream(stream, mime, filename);
    }
}