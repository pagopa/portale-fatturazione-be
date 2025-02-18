using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

public class FattureWhiteListFattureAggiungiCommandPersistence(FattureWhiteListFattureAggiungiCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly FattureWhiteListFattureAggiungiCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlInserite = @"
INSERT INTO [pfd].[FattureWhiteList]
           ([FkIdEnte]
           ,[Anno]
           ,[Mese]
           ,[DataInizio]
           ,[FkTipologiaFattura]
           ,[IdUtente])
     VALUES
           (@IdEnte
           ,@Anno
           ,@Mese
           ,@DataInizio
           ,@TipologiaFattura
           ,@IdUtente); ";


    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var parameters = _command.Mesi!.Select(mese => new
        {
            IdEnte = _command.IdEnte,
            Anno = _command.Anno,
            Mese = mese,
            DataInizio = _command.DataInizio,
            TipologiaFattura = _command.TipologiaFattura,
            IdUtente = _command.IdUtente
        });
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInserite, parameters, transaction);
    }
} 