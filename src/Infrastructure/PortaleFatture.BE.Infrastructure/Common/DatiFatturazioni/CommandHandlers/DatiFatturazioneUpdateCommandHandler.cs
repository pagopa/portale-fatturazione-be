using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.CommandHandlers;

public class DatiFatturazioneUpdateCommandHandler : IRequestHandler<DatiFatturazioneUpdateCommand, DatiFatturazione>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<DatiFatturazioneUpdateCommandHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;
    public DatiFatturazioneUpdateCommandHandler(
         IFattureDbContextFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<DatiFatturazioneUpdateCommandHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<DatiFatturazione> Handle(DatiFatturazioneUpdateCommand command, CancellationToken ct)
    {
        var (anno, mese, adesso) = Time.YearMonth();

        var (error, errorDetails) = DatiFatturazioneValidator.Validate(command);
        if (!string.IsNullOrEmpty(error))
            throw new ValidationException(_localizer[error, errorDetails]);
 
        command.DataModifica = command.DataModifica == null ? adesso : command.DataModifica; 

        using var uow = await _factory.Create(true, cancellationToken: ct);
        var actualValue = await uow.Query(new DatiFatturazioneQueryGetByIdPersistence(command.Id!), ct) ?? throw new DomainException(_localizer["DatiFatturazioneIdExistent", command.Id!]);
     
        //verifico coerenza dati
        if(actualValue.IdEnte!= command.AuthenticationInfo.IdEnte || actualValue.Prodotto!= command.AuthenticationInfo.Prodotto)
            throw new DomainException(_localizer["DatiFatturazioneMismatchError"]);

        try
        {
            var updatedDatiFatturazione = await uow.Execute(new DatiFatturazioneUpdateCommandPersistence(command), ct) ??
                throw new DomainException(_localizer["DatiFatturazioneUpdateError"]); 
       
            if (!command.Contatti!.IsNullNotAny())
            {
                var rowAffected = 0;
                await uow.Execute(new DatiFatturazioneContattoDeleteCommandPersistence(new DatiFatturazioneContattoDeleteCommand() { IdDatiFatturazione = command.Id }), ct);
          
                foreach (var commandContatto in command.Contatti!)
                    commandContatto.IdDatiFatturazione = command.Id!;

                rowAffected += await uow.Execute(new DatiFatturazioneContattoCreateListCommandPersistence(command.Contatti!), ct);

                if (rowAffected != command.Contatti.Count)
                {
                    uow.Rollback();
                    throw new DomainException(_localizer["DatiFatturazioneInputError"]);
                }
            }
            updatedDatiFatturazione.Contatti = command.Contatti!.Mapper();
            uow.Commit();
            return updatedDatiFatturazione;
        }
        catch (Exception e)
        {
            uow.Rollback();
            var methodName = nameof(DatiFatturazioneUpdateCommandHandler);
            _logger.LogError(e, "Errore nel commando: \"{MethodName}\" per dati commessa id: \"{Id}\"", methodName, command.Id);
            throw new DomainException(_localizer["DatiFatturazioneUpdateError"]);
        }
    }
}