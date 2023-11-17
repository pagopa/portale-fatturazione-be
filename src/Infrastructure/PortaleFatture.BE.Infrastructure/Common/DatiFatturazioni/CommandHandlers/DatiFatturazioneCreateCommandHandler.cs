using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.CommandHandlers;

public class DatiFatturazioneCreateCommandHandler : IRequestHandler<DatiFatturazioneCreateCommand, DatiFatturazione>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<DatiFatturazioneCreateCommandHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;
    public DatiFatturazioneCreateCommandHandler(
         IFattureDbContextFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<DatiFatturazioneCreateCommandHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<DatiFatturazione> Handle(DatiFatturazioneCreateCommand command, CancellationToken ct)
    { 
        var (error, errorDetails) = DatiFatturazioneValidator.Validate(command);
        if (!string.IsNullOrEmpty(error))
            throw new DomainException(_localizer[error, errorDetails]); 
 
        command.DataCreazione = command.DataCreazione == null ? DateTime.UtcNow.ItalianTime() : command.DataCreazione;
        using var uow = await _factory.Create(true, cancellationToken: ct);
        var products = await uow.Query(new ProdottoQueryGetAllPersistence(), ct);
        command.Prodotto = products.FirstOrDefault()!.Nome;
     
        // verifico se esiste già un valore per l'ente
        var actualValue = await uow.Query(new DatiFatturazioneQueryGetByIdEntePersistence(command.IdEnte!), ct);
        if (actualValue != null)
            throw new DomainException(_localizer["DatiFatturazioneIdEnteExistent", command.IdEnte!]);
        try
        {
            var id = await uow.Execute(new DatiFatturazioneCreateCommandPersistence(command), ct); 
            if (!command.Contatti!.IsNullNotAny())
            {
                var rowAffected = 0; 
                foreach (var commandContatto in command.Contatti!) 
                    commandContatto.IdDatiFatturazione = id;  

                rowAffected += await uow.Execute(new DatiFatturazioneContattoCreateListCommandPersistence(command.Contatti!), ct);
                
                if (rowAffected != command.Contatti.Count)
                {
                    uow.Rollback();
                    throw new DomainException(_localizer["DatiFatturazioneInputError"]);
                }
            }
            var actualDatiFatturazione = command.Mapper(id);
            uow.Commit();
            return actualDatiFatturazione;
        }
        catch (Exception e)
        {
            var methodName = nameof(DatiFatturazioneCreateCommandHandler);
            _logger.LogError(e, "Errore nel commando: \"{MethodName}\" per ente: \"{IdEnte}\"", methodName, command.IdEnte);
            throw new DomainException(_localizer["DatiFatturazioneInputError"]);
        }
    }
}