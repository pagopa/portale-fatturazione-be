using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.Commands.Persistence;

public class ContestazioneCreateCommandPersistence(ContestazioneCreateCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly ContestazioneCreateCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlInsert = @"
INSERT INTO [schema][Contestazioni]
           ([FkIdTipoContestazione]
           ,[NoteEnte] 
           ,[FkIdFlagContestazione] 
           ,[DataInserimentoEnte]
           ,[FkIdNotifica]
           ,[Anno]
           ,[Mese])
     VALUES 
           (@TipoContestazione,
            @NoteEnte,
            @StatoContestazione, 
            @DataInserimentoEnte,
            @IdNotifica,
            @Anno,
            @Mese);
Select SCOPE_IDENTITY() 'SCOPE_IDENTITY';
";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert.Add(schema), _command, transaction);
        } 
        catch (SqlException ex)
        {
            if (ex.Number == 2627) // 2627 - Violation in unique constraint 
                throw new DomainException(_localizer["CreazioneContestazioneDuplicate", _command.IdNotifica!]);
            else
                throw new DomainException(_localizer["CreazioneContestazioneError", _command.IdNotifica!]);
        } 
    }
}