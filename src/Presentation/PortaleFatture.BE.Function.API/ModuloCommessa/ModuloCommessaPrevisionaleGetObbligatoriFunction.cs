using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;

namespace PortaleFatture.BE.Function.API.ModuloCommessa;

public class ModuloCommessaPrevisionaleGetObbligatoriFunction(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ModuloCommessaPrevisionaleGetObbligatoriFunction>();

    [Function("ModuloCommessaPrevisionaleGetObbligatori")]
    public async Task<ModuloCommessaPrevisionaleObbligatoriResponse> RunAsync(
        [ActivityTrigger] ModuloCommessaRegioneInternalRequest ireq,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        var localizer = context.InstanceServices.GetRequiredService<IStringLocalizer<Localization>>();

        int? idTipoContratto = ireq.Session!.IdTipoContratto;

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = ireq.Session!.IdEnte,
            Prodotto = ireq.Session!.Prodotto,
            IdTipoContratto = idTipoContratto,
        };

        var modulo = await mediator.Send(new DatiModuloCommessaPrevisionaleQueryGetByAnno(authInfo)
        {
            AnnoValidita = null,
        });

        if (modulo.IsNullNotAny())
            throw new Exception(localizer[MessageHelper.NotFound]);

        var filtered = modulo!
            .Where(x => x.Source!.Contains("obbliga", StringComparison.CurrentCultureIgnoreCase))
            .ToList();

        var lista = filtered.Any(x => !x.Totale.HasValue)
            ? filtered
            : [];

        if (lista.IsNullNotAny())
            throw new Exception(localizer[MessageHelper.NotFound]);

        var aderente = await mediator.Send(new DatiModuloCommessaAderentiQueryGet(authInfo)
        {
            IdEnte = authInfo.IdEnte,
        });

        var obbligatori = new ModuloCommessaPrevisionaleObbligatoriResponse()
        {
            DescrizioneMacrocategoriaVendita = aderente.SottocategoriaVendita,
            MacrocategoriaVendita = aderente.TipoDistribuzione,
            Lista = lista
        }; 

        var sse = ireq.Session;
        sse.Payload = obbligatori.Serialize();
        var logResponse = context.Response(sse);

        try
        {
            await mediator.Send(logResponse);
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"{LoggerHelper.PREFIX}{MessageHelper.BadRequestLogging}{LoggerHelper.PIPE}{ex.Serialize()}");
            throw new DomainException(MessageHelper.BadRequestLogging, ex);
        }

        return obbligatori!;
    }
}