using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Service;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.QueryHandlers;
public class FattureQueryInvioSapHandler(
 ISelfCareDbContextFactory factory,
 IServiceWorkFlowFatture workFlowService,
 IStringLocalizer<Localization> localizer,
 IMediator handler,
 ILogger<FattureQueryInvioSapHandler> logger) : IRequestHandler<FattureInvioSapQuery, IEnumerable<FatturaInvioSap>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<FattureQueryInvioSapHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IServiceWorkFlowFatture _workFlowService = workFlowService;
    private readonly IMediator _handler = handler;
    public async Task<IEnumerable<FatturaInvioSap>?> Handle(FattureInvioSapQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        var fatture = await rs.Query(new FattureQueryInvioSapPersistence(request), ct);

        if (!fatture.IsNullNotAny())
        {
            foreach (var value in fatture!)
            {
                var workflow = fatture.ToWorkFlow(_workFlowService);
                var wfqueryCommand = new FattureWorkFlowQuery(request.AuthenticationInfo, workflow);
                var conditions = await _handler.Send(wfqueryCommand);

                foreach (var fatt in fatture!)
                {
                    var condition = conditions!.
                            Where(x => x.TipologiaFattura == fatt.TipologiaFattura)
                            .Select(x => (x.ExtraCondition!.Value == 0 ? x.ExtraCondition!.Value : x.Modifica!.Value))
                            .FirstOrDefault();

                    fatt.Azione = condition == 2 ? condition : fatt.Azione;
                }
            }
        }
        return fatture;
    }
}