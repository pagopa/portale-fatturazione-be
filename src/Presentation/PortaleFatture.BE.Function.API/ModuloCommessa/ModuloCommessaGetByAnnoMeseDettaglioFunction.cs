using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;

namespace PortaleFatture.BE.Function.API.ModuloCommessa;

public class ModuloCommessaGetByAnnoMeseDettaglioFunction(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ModuloCommessaGetByAnnoMeseDettaglioFunction>();

    [Function("ModuloCommessaGetByAnnoMeseDettaglio")]
    public async Task<ModuloCommessaPrevisionaleObbligatoriResponse> RunAsync(
        [ActivityTrigger] ModuloCommessaGetByAnnoMeseDettaglioInternalRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = req.Session!.IdEnte,
            Prodotto = req.Session!.Prodotto,
        };

        var anno = req.Anno;
        var mese = req.Mese;

        var moduli = await mediator.Send(new DatiModuloCommessaPrevisionaleQueryGetByAnno(authInfo)
        {
            AnnoValidita = anno,
            MeseValidita = mese
        });

        if (moduli.IsNullNotAny())
            throw new Exception("Modulo Commessa non trovato!!!");

        var modulo = moduli!.FirstOrDefault();
        var aderente = await mediator.Send(new DatiModuloCommessaAderentiQueryGet(authInfo)
        {
            IdEnte = authInfo.IdEnte,
        });

        var totali = await mediator.Send(new DatiModuloCommessaTotaleQueryGet(authInfo)
        {
            AnnoValidita = anno,
            MeseValidita = mese
        });


        var previsonale = new ModuloCommessaPrevisionaleObbligatoriResponse()
        {
            DescrizioneMacrocategoriaVendita = aderente.SottocategoriaVendita,
            MacrocategoriaVendita = aderente.TipoDistribuzione,
            Lista = [modulo],
            Totali = totali!.ToList()
        };

        var sse = req.Session;

        sse.Payload = previsonale.Serialize();

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
        return previsonale!;
    }
}