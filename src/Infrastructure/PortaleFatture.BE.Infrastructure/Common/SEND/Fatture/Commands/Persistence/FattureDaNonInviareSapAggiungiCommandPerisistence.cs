using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

public class FattureDaNonInviareSapAggiungiCommandPersistence(FattureDaNonInviareSapAggiungiCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly FattureDaNonInviareSapAggiungiCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlInserite = @"
INSERT INTO [cfg].[FattureStaging]
           ([FkIdEnte]
           ,[Anno]
           ,[Mese]
           ,[DataInserimento]
           ,[FkTipologiaFattura]
           ,[IdUtenteInserimento]
           ,[Stato])
     VALUES
           (@IdEnte
           ,@Anno
           ,@Mese
           ,@DataInserimento
           ,@TipologiaFattura
           ,@IdUtente
           ,@Stato
); ";


    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var parameters = _command.Mesi!.Select(mese => new
        {
            IdEnte = _command.IdEnte,
            Anno = _command.Anno,
            Mese = mese,
            DataInserimento = _command.DataInserimento,
            TipologiaFattura = _command.TipologiaFattura,
            IdUtente = _command.IdUtente,
            Stato = _command.Stato
        });
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInserite, parameters, transaction);
    }
}
