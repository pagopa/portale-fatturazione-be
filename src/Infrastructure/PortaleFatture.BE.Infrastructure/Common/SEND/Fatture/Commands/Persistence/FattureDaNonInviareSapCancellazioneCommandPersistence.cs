using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

public class FattureDaNonInviareSapCancellazioneCommandPersistence(FattureDaNonInviareSapCancellazioneCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly FattureDaNonInviareSapCancellazioneCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlEliminate = @"
    UPDATE [cfg].[FattureStaging]
     SET 
    [DataCancellazione] = @DataCancellazione,
    [IdUtenteCancellazione] = @IdUtenteCancellazione,
    [Stato] = 2
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
            DataCancellazione = _command.DataCancellazione,
            IdUtenteCancellazione = _command.IdUtenteCancellazione,
            fattura.IdEnte,
            fattura.TipologiaFattura,
            fattura.Anno,
            fattura.Mese,
            fattura.Stato
        }).ToList();

        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlEliminate, parameters, transaction);
    }

}