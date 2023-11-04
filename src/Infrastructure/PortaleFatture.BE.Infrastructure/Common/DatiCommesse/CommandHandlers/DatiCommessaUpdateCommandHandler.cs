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

public class DatiCommessaUpdateCommandHandler : IRequestHandler<DatiCommessaUpdateCommand, DatiCommessa>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly ILogger<DatiCommessaUpdateCommandHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;
    public DatiCommessaUpdateCommandHandler(
         IUnitOfWorkFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<DatiCommessaUpdateCommandHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<DatiCommessa> Handle(DatiCommessaUpdateCommand command, CancellationToken ct)
    {
        var (error, errorDetails) = DatiCommessaValidator.Validate(command);
        if (!string.IsNullOrEmpty(error))
            throw new ValidationException(_localizer[error, errorDetails]);

        var actualDateTime = DateTime.UtcNow.ActualTime();
        command.DataModifica = actualDateTime;
        try
        {
            using var uow = await _factory.Create(true, cancellationToken: ct);
            var updatedDatiCommessa = await uow.Execute(new DatiCommessaUpdateCommandPersistence(command), ct) ?? 
                throw new DomainException(_localizer["DatiCommessaUpdateError"]);

            if (!command.Contatti!.IsNullNotAny())
            {
                await uow.Execute(new DatiCommessaContattoDeleteCommandPersistence(new DatiCommessaContattoDeleteCommand() { IdDatiCommessa = command.Id }), ct);
                var rowAffected = 0;
                foreach (var commandContatto in command.Contatti!)
                {
                    commandContatto.IdDatiCommessa = command.Id;
                    rowAffected += await uow.Execute(new DatiCommessaContattoCreateCommandPersistence(commandContatto), ct);
                } 
                if (rowAffected != command.Contatti!.Count)
                {
                    uow.Rollback();
                    throw new DomainException(_localizer["DatiCommessaUpdateError"]);
                }
            }
            updatedDatiCommessa.Contatti = command.Contatti!.Select(x => x.Mapper());
            uow.Commit(); 
            return updatedDatiCommessa;
        }
        catch (Exception e)
        {
            var methodName = nameof(DatiCommessaUpdateCommandHandler);
            _logger.LogError(e, "Errore nel commando: \"{MethodName}\" per dati commessa id: \"{Id}\"", methodName, command.Id);
            throw new DomainException(_localizer["DatiCommessaUpdateError"]);
        }
    }
}