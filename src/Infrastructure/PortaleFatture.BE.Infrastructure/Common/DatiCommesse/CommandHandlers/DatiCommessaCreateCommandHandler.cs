using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiCommesse;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.CommandHandlers;

public class DatiCommessaCreateCommandHandler : IRequestHandler<DatiCommessaCreateCommand, DatiCommessa>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly ILogger<DatiCommessaCreateCommandHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;
    public DatiCommessaCreateCommandHandler(
         IUnitOfWorkFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<DatiCommessaCreateCommandHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<DatiCommessa> Handle(DatiCommessaCreateCommand command, CancellationToken ct)
    {
        var (error, errorDetails) = DatiCommessaValidator.Validate(command);
        if (!string.IsNullOrEmpty(error))
            throw new DomainException(_localizer[error, errorDetails]);

        var actualDateTime = DateTime.UtcNow.ActualTime();
        command.DataCreazione = actualDateTime;
        try
        {
            using var uow = await _factory.Create(true, cancellationToken: ct);
            var id = await uow.Execute(new DatiCommessaCreateCommandPersistence(command), ct);
            command.Id = id;
            if (!command.Contatti!.IsNullNotAny())
            {
                var rowAffected = 0;
                foreach (var commandContatto in command.Contatti!)
                {
                    commandContatto.IdDatiCommessa = id;
                    rowAffected += await uow.Execute(new DatiCommessaContattoCreateCommandPersistence(commandContatto), ct);
                }
                if (rowAffected != command.Contatti.Count)
                {
                    uow.Rollback();
                    throw new DomainException(_localizer["DatiCommessaInputError"]);
                }
            }
            var actualDatiCommessa = command.Mapper();
            uow.Commit();
            return actualDatiCommessa;
        }
        catch (Exception e)
        {
            var methodName = nameof(DatiCommessaCreateCommandHandler);
            _logger.LogError(e, "Errore nel commando: \"{MethodName}\" per ente: \"{IdEnte}\"", methodName, command.IdEnte);
            throw new DomainException(_localizer["DatiCommessaInputError"]);
        }
    }
}