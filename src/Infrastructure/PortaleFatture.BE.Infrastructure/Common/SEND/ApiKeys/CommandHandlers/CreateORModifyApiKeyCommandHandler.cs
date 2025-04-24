using System.Security;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.CommandHandlers;

public class CreateORModifyApiKeyCommandHandler(
    ISelfCareDbContextFactory factory,
    IStringLocalizer<Localization> localizer,
    IMediator handler,
    ILogger<CreateORModifyApiKeyCommandHandler> logger) : IRequestHandler<CreateORModifyApiKeyCommand, int?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<CreateORModifyApiKeyCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IMediator _handler = handler;
    public async Task<int?> Handle(CreateORModifyApiKeyCommand request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.ApiKey))
            request.ApiKey = Guid.NewGuid().ToString();
        if (!request.Attiva.HasValue)
            request.Attiva = true;
        if (request.Refresh.HasValue && request.Refresh == true)
        {
            request.PreviousApiKey = request.ApiKey;
            request.ApiKey = Guid.NewGuid().ToString();
        }

        using var rs = await _factory.Create(true, cancellationToken: ct);
        {
            var result = await rs.Query(new CheckApiKeyQueryGetPersistence(new ApiKeyQueryGet(request!.AuthenticationInfo!) { }));
            if (result.IsNullNotAny())
                throw new SecurityException("Ente non registrato!");
            var rowAffected = await rs.Query(new CreateORModifyApiKeyPersistence(request), ct);
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