using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.pagoPA.PspEmail.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.InvioPsp.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.InvioPsp.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.pagoPA.PspEmail;
 
public partial class PspEmailModule : Module, IRegistrableModule
{ 

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<PspEmailDto>>, NotFound>> PostPspEmail(
     HttpContext context,
     [FromBody] PspEmailRequest request,
     [FromServices] IMediator handler,
     [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var pspEmails = await handler.Send(new PspEmailQueryGet(authInfo)
        { 
            Quarters = request.Quarters, 
            Year = request.Year, 
            ContractIds = request.ContractIds
        });

        if (pspEmails.IsNullNotAny())
            return NotFound();

        return Ok(pspEmails);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostPspEmailDocument(
    HttpContext context,
    [FromBody] PspEmailRequest request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var pspEmails = await handler.Send(new PspEmailQueryGet(authInfo)
        {
            Quarters = request.Quarters,
            Year = request.Year,
            ContractIds = request.ContractIds
        });

        if (pspEmails.IsNullNotAny())
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx"; 
  
        var dataSet = pspEmails!.FillPagoPAOneSheet("Psp Email");
        var content = dataSet.ToExcel();

        content.Seek(0, SeekOrigin.Begin);
        return Results.Stream(content!, mime, filename);
    }
}