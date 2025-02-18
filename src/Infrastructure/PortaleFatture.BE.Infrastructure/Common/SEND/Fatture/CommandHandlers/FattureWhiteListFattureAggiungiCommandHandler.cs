using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.CommandHandlers;

internal class FattureWhiteListFattureAggiungiCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<FattureWhiteListFattureAggiungiCommandHandler> logger) : IRequestHandler<FattureWhiteListFattureAggiungiCommand, bool?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<FattureWhiteListFattureAggiungiCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<bool?> Handle(FattureWhiteListFattureAggiungiCommand command, CancellationToken ct)
    {
        using var uow = await _factory.Create(true, cancellationToken: ct);
        try
        {
            var rowAffected = await uow.Execute(new FattureWhiteListFattureAggiungiCommandPersistence(command, _localizer), ct);
            if (rowAffected == command.Mesi!.Length)
            {
                uow.Commit();
                return true;
            }
            else
            {
                uow.Rollback();
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            uow.Rollback();
            return false;
        } 
    }
}