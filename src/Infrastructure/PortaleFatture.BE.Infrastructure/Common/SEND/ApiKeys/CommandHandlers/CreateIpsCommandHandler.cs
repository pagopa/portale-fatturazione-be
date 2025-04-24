using System.Security;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.CommandHandlers;

public class CreateIpsCommandHandler(
    ISelfCareDbContextFactory factory,
    IStringLocalizer<Localization> localizer,
    IMediator handler,
    ILogger<CreateIpsCommandHandler> logger) : IRequestHandler<CreateIpsCommand, int?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<CreateIpsCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IMediator _handler = handler;
    public async Task<int?> Handle(CreateIpsCommand request, CancellationToken ct)
    { 
        using var rs = await _factory.Create(true, cancellationToken: ct);
        {
            var result = await rs.Query(new CheckApiKeyQueryGetPersistence(new ApiKeyQueryGet(request!.AuthenticationInfo!) { }));
            if (result.IsNullNotAny())
                throw new SecurityException("Ente non registrato!");
            var rowAffected = await rs.Query(new CreateIpsCommandPersistence(request), ct);
            if (rowAffected.HasValue && rowAffected > 0)
            {
                rs.Commit();
                return rowAffected;
            } 
            else
            {
                rs.Rollback();
                return rowAffected;
            } 
        }
    }
}