using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;
public class FattureQueryRicercaHandler(
 IMediator handler,
 ISelfCareDbContextFactory factory,
 IServiceWorkFlowFatture workFlowService,
 IStringLocalizer<Localization> localizer,
 ILogger<FattureQueryRicercaHandler> logger) : IRequestHandler<FattureQueryRicerca, FattureListaDto?>
{
    private readonly IMediator _handler = handler;
    private readonly IServiceWorkFlowFatture _workFlowService = workFlowService;
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<FattureQueryRicercaHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<FattureListaDto?> Handle(FattureQueryRicerca request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        var fatture = await rs.Query(new FattureQueryRicercaPersistence(request), ct);

        if (!fatture.IsNullNotAny())
        {
            var workflow = fatture.ToWorkFlow(_workFlowService);
            var wfqueryCommand = new FattureWorkFlowQuery(request.AuthenticationInfo, workflow);
            var conditions = await _handler.Send(wfqueryCommand, ct);

            foreach (var fatt in fatture!)
            {
                //fatt.fattura!.Elaborazione = fatt.fattura!.Inviata == 2 ? fatt.fattura!.Inviata : conditions!.
                //    Where(x => x.TipologiaFattura == fatt.fattura.TipologiaFattura)
                //    .Select(x => x.ExtraCondition!.Value == 0 ? x.ExtraCondition!.Value : x.Modifica!.Value)
                //    .FirstOrDefault();
                fatt.fattura!.Elaborazione = 0;
                //
            }
        }
        return fatture;
    }
}