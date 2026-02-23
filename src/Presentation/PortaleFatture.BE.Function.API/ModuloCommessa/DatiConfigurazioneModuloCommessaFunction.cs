using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Api.Modules.SEND.DatiConfigurazioneModuloCommesse.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.DatiConfigurazioneModuloCommesse.Response;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;

namespace PortaleFatture.BE.Function.API.ModuloCommessa;

public class DatiConfigurazioneModuloCommessaFunction(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<DatiConfigurazioneModuloCommessaFunction>();

    [Function("DatiConfigurazioneModuloCommessa")]
    public async Task<DatiConfigurazioneModuloCommessaResponse> RunAsync(
        [ActivityTrigger] DatiConfigurazioneModuloCommessaRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>(); 

        int? idTipoContratto = req.Session!.IdTipoContratto;
        string? prodotto = req.Session!.Prodotto;
        var configurazione = await mediator.Send(new DatiConfigurazioneModuloCommessaQueryGet() { Prodotto = prodotto, IdTipoContratto = idTipoContratto!.Value });

        var conf = configurazione.Mapper();

        var sse = req.Session;
        sse!.Payload = conf.Serialize();
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
        return conf;
    }
}