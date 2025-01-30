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

public class DatiFatturazioneCreateCommandHandler(
    IFattureDbContextFactory factory,
    ISelfCareOnBoardingHttpClient onBoardingHttpClient,
    ISupportAPIServiceHttpClient supportAPIServiceHttpClient, 
    IStringLocalizer<Localization> localizer,
    ILogger<DatiFatturazioneCreateCommandHandler> logger) : IRequestHandler<DatiFatturazioneCreateCommand, DatiFatturazione>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<DatiFatturazioneCreateCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer; 
    private readonly ISelfCareOnBoardingHttpClient _onBoardingHttpClient = onBoardingHttpClient;
    private readonly ISupportAPIServiceHttpClient _supportAPIServiceHttpClient = supportAPIServiceHttpClient;

    public async Task<DatiFatturazione> Handle(DatiFatturazioneCreateCommand command, CancellationToken ct)
    {
        var (_, _, _, adesso) = Time.YearMonthDay();

        var (error, errorDetails) = DatiFatturazioneValidator.Validate(command);
        if (!string.IsNullOrEmpty(error))
            throw new ValidationException(_localizer[error, errorDetails]);

        command.DataCreazione = command.DataCreazione == null ? adesso : command.DataCreazione; 

        using var uow = await _factory.Create(true, cancellationToken: ct); 
        var actualValue = await uow.Query(new DatiFatturazioneQueryGetByIdEntePersistence(command.AuthenticationInfo.IdEnte!), ct);
        if (actualValue != null)
            throw new DomainException(_localizer["DatiFatturazioneIdEnteExistent", command.AuthenticationInfo.IdEnte!]);

        if (String.IsNullOrEmpty(command.CodiceSDI))
            throw new ValidationException("Attenzione, devi validare il codice SDI"); //non puoi cancellare il codice SDI 
        else
        {
            var contratto = await uow.Query(new EnteCodiceSDIQueryGetByIdPersistence(command.AuthenticationInfo.IdEnte), ct);
            var skipVerifica = (command.CodiceSDI == contratto.CodiceSDI);
            var ente = await uow.Query(new EnteCodiceSDIQueryGetByIdPersistence(command.AuthenticationInfo.IdEnte), ct);
            var (okValidation, msgValidation) = await _onBoardingHttpClient.RecipientCodeVerification(
                ente,
                command.CodiceSDI,
                skipVerifica,
                ct);

            if (okValidation)
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
            var rowAffected = 0;
            var id = await uow.Execute(new DatiFatturazioneCreateCommandPersistence(command), ct);
            if (id == null)
            {
                uow.Rollback();
                throw new DomainException(_localizer["DatiFatturazioneInputError"]);
            }

            if (!command.Contatti!.IsNullNotAny())
            {
                foreach (var commandContatto in command.Contatti!)
                    commandContatto.IdDatiFatturazione = id.Value;

                rowAffected += await uow.Execute(new DatiFatturazioneContattoCreateListCommandPersistence(command.Contatti!), ct);

                if (rowAffected != command.Contatti.Count)
                {
                    uow.Rollback();
                    throw new DomainException(_localizer["DatiFatturazioneInputError"]);
                }
            }

            var actualDatiFatturazione = command.Mapper(id.Value);
            rowAffected = await uow.Execute(new StoricoCreateCommandPersistence(new StoricoCreateCommand(
                command.AuthenticationInfo,
                adesso,
                TipoStorico.DatiFatturazione,
                actualDatiFatturazione.Serialize())), ct);

            if (rowAffected == 1)
                uow.Commit();
            else
            {
                uow.Rollback();
                throw new DomainException(_localizer["DatiFatturazioneInputError"]);
            }

            return actualDatiFatturazione;
        }
        catch (Exception e)
        {
            uow.Rollback();
            var methodName = nameof(DatiFatturazioneCreateCommandHandler);
            _logger.LogError(e, "Errore nel commando: \"{MethodName}\" per ente: \"{IdEnte}\"", methodName, command.AuthenticationInfo.IdEnte);
            throw new DomainException(_localizer["DatiFatturazioneInputError"]);
        }
    }
}