using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

public class FattureDaNonInviareSapRipristinoCommandPersistence(FattureDaNonInviareSapRipristinoCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{

    public bool RequiresTransaction => false;
    private readonly FattureDaNonInviareSapRipristinoCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlRipristinate = @"
    UPDATE [cfg].[FattureStaging]
     SET 
    [DataRipristino] = @DataRipristino,
    [IdUtenteRipristino] = @IdUtenteRipristino,
    [Stato] = 1
     WHERE 
    FkIdEnte = @IdEnte
    AND FkTipologiaFattura = @TipologiaFattura
    AND Anno = @Anno
    AND Mese = @Mese
    AND Stato = 0";


    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var parameters = _command.Fatture.Select(fattura => new
        {
            DataRipristino = _command.DataRipristino,
            IdUtenteRipristino = _command.IdUtenteRipristino,
            fattura.IdEnte,
            fattura.TipologiaFattura,
            fattura.Anno,
            fattura.Mese,
            fattura.Stato
        }).ToList();

        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlRipristinate, parameters, transaction);
    }

}

