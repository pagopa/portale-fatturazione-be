using System.Security;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.Storici;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.CommandHandlers;

public class CreateORModifyApiKeyCommandHandler(
    IFattureDbContextFactory factory,
    IStringLocalizer<Localization> localizer,
    IAesEncryption encryption,
    ILogger<CreateORModifyApiKeyCommandHandler> logger) : IRequestHandler<CreateORModifyApiKeyCommand, int?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<CreateORModifyApiKeyCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IAesEncryption _encryption = encryption;
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
            var result = await rs.Query(new CheckApiKeyQueryGetPersistence(new ApiKeyQueryGet(request!.AuthenticationInfo!) { }, _encryption), ct);
            if (result.IsNullNotAny())
                throw new SecurityException("Ente non registrato!");

            request.ApiKey = request.ApiKey == null ? null : _encryption.EncryptString(request.ApiKey);
            request.PreviousApiKey = request.PreviousApiKey == null ? null : _encryption.EncryptString(request.PreviousApiKey); 

            var rowAffected = await rs.Query(new CreateORModifyApiKeyPersistence(request), ct);
            if (rowAffected.HasValue && rowAffected > 0)
            {
                rowAffected = await rs.Execute(new StoricoCreateCommandPersistence(new StoricoCreateCommand(
                              request.AuthenticationInfo!,
                              DateTime.UtcNow.ItalianTime(),
                              TipoStorico.CreaAPIKey,
                              request.Serialize())), ct);
                if (rowAffected == 1)
                    rs.Commit();
                else
                    rs.Rollback();
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