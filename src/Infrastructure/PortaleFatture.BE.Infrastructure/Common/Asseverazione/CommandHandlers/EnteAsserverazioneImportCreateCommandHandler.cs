using System.Security;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Asseverazione.Dto;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.CommandHandlers;

public class EnteAsserverazioneImportCreateCommandHandler(
 ISelfCareDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<EnteAsserverazioneImportCreateCommandHandler> logger) : IRequestHandler<EnteAsserverazioneListImportCreateCommand, bool>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<EnteAsserverazioneImportCreateCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<bool> Handle(EnteAsserverazioneListImportCreateCommand command, CancellationToken ct)
    {
        var authInfo = command!.AuthenticationInfo!;

        if (!(authInfo.Profilo == Profilo.Approvigionamento ||
            authInfo.Profilo == Profilo.Finanza ||
            authInfo.Profilo == Profilo.Assistenza
            ))
            throw new SecurityException(); //401  

        using var uow = await _factory.Create(cancellationToken: ct); 
            return await uow.Execute(new EnteAsserverazioneImportCreateCommandPersistence(command.ListCommands!, _localizer), ct) > 0;  
    }
}