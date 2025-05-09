using CsvHelper;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence;

namespace PortaleFatture.BE.Function.API.ModuloCommessa;

public class DatiModuloCommessaCreateFunction(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<DatiModuloCommessaCreateFunction>();

    [Function("DatiModuloCommessaCreate")]
    public async Task<DatiModuloCommessaResponse> RunAsync(
        [ActivityTrigger] DatiModuloCommessaCreateInternalRequest ireq, 
        FunctionContext context)
    { 
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        var factory = context.InstanceServices.GetRequiredService<IFattureDbContextFactory>();

        int? idTipoContratto;
        using var uow = await factory.Create();
        {
            var contratto = await uow.Query(new EnteCodiceSDIQueryGetByIdPersistence(ireq.Session!.FkIdEnte));
            idTipoContratto = contratto?.IdTipoContratto;
        } 

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = ireq.Session!.FkIdEnte,
            Prodotto = "prod-pn",
            IdTipoContratto = idTipoContratto,
        };

        var req = ireq.Map();
        var command = req.Mapper(authInfo) ?? throw new Core.Exceptions.ValidationException("");
        foreach (var cmd in command.DatiModuloCommessaListCommand!)
            cmd.IdEnte = ireq.Session!.FkIdEnte;

        var modulo = await mediator.Send(command) ?? throw new DomainException("");
        var response = modulo!.Mapper(authInfo);

        var sse = ireq.Session;
        sse.Payload = response.Serialize();
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

        return response!; 
    }
}