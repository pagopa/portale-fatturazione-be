using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Entities.SEND.DatiFatturazioni;
using PortaleFatture.BE.Core.Entities.Storici;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.CommandHandlers;

public class DatiFatturazioneUpdateCommandHandler(
     IFattureDbContextFactory factory,
     ISelfCareOnBoardingHttpClient onBoardingHttpClient,
     ISupportAPIServiceHttpClient supportAPIServiceHttpClient, 
     IStringLocalizer<Localization> localizer,
     ILogger<DatiFatturazioneUpdateCommandHandler> logger) : IRequestHandler<DatiFatturazioneUpdateCommand, DatiFatturazione>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<DatiFatturazioneUpdateCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly ISelfCareOnBoardingHttpClient _onBoardingHttpClient = onBoardingHttpClient;
    private readonly ISupportAPIServiceHttpClient _supportAPIServiceHttpClient = supportAPIServiceHttpClient; 
    public async Task<DatiFatturazione> Handle(DatiFatturazioneUpdateCommand command, CancellationToken ct)
    {
        var (_, _, _, adesso) = Time.YearMonthDay();

        var (error, errorDetails) = DatiFatturazioneValidator.Validate(command);
        if (!string.IsNullOrEmpty(error))
            throw new ValidationException(_localizer[error, errorDetails]);

        command.DataModifica = command.DataModifica == null ? adesso : command.DataModifica;


        using var uow = await _factory.Create(true, cancellationToken: ct);
        var actualValue = await uow.Query(new DatiFatturazioneQueryGetByIdPersistence(command.Id!), ct) ?? throw new DomainException(_localizer["DatiFatturazioneIdExistent", command.Id!]);

        //verifico coerenza dati
        if (actualValue.IdEnte != command.AuthenticationInfo.IdEnte || actualValue.Prodotto != command.AuthenticationInfo.Prodotto)
            throw new DomainException(_localizer["DatiFatturazioneMismatchError"]); 

        if (String.IsNullOrEmpty(command.CodiceSDI))
            throw new ValidationException("Attenzione, devi validare il codice SDI"); //non puoi cancellare il codice SDI

        else if (!String.IsNullOrEmpty(command.CodiceSDI) && command.CodiceSDI != actualValue.CodiceSDI) // se sono diversi allora rieseguo la validazione via API
        { 
            var ente = await uow.Query(new EnteCodiceSDIQueryGetByIdPersistence(actualValue.IdEnte), ct); 
            var (okValidation, msgValidation) = await _onBoardingHttpClient.RecipientCodeVerification(
                ente, 
                command.CodiceSDI, 
                ct);

            if(okValidation)
            {
                var (oKUpdate, msgUpdate) = await _supportAPIServiceHttpClient.UpdateRecipientCode(
                    ente,
                    command.CodiceSDI,
                    ct);
                if (!oKUpdate)
                    throw new ValidationException(msgUpdate);
            }
            else 
                throw new ValidationException(msgValidation); 
        } 

        try
        {
            var updatedDatiFatturazione = await uow.Execute(new DatiFatturazioneUpdateCommandPersistence(command), ct) ??
                throw new DomainException(_localizer["DatiFatturazioneUpdateError"]);

            var rowAffected = 0;

            await uow.Execute(new DatiFatturazioneContattoDeleteCommandPersistence(new DatiFatturazioneContattoDeleteCommand() { IdDatiFatturazione = command.Id }), ct);

            if (!command.Contatti!.IsNullNotAny())
            {
                foreach (var commandContatto in command.Contatti!)
                    commandContatto.IdDatiFatturazione = command.Id!;

                rowAffected += await uow.Execute(new DatiFatturazioneContattoCreateListCommandPersistence(command.Contatti!), ct);

                if (rowAffected != command.Contatti.Count)
                {
                    uow.Rollback();
                    throw new DomainException(_localizer["DatiFatturazioneInputError"]);
                }
                updatedDatiFatturazione.Contatti = command.Contatti!.Mapper();
            }

            rowAffected = await uow.Execute(new StoricoCreateCommandPersistence(new StoricoCreateCommand(
                command.AuthenticationInfo,
                adesso,
                TipoStorico.DatiFatturazione,
                updatedDatiFatturazione.Serialize())), ct);

            if (rowAffected == 1)
                uow.Commit();
            else
            {
                uow.Rollback();
                throw new DomainException(_localizer["DatiFatturazioneInputError"]);
            }
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