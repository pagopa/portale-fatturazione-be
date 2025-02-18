using System.Data;
using Microsoft.Extensions.Localization;
using Org.BouncyCastle.Crypto;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

public class FatturaWhiteListCancellazioneCommandPersistence(FatturaWhiteListCancellazioneCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly FatturaWhiteListCancellazioneCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlEliminate = @"
    UPDATE [pfd].[FattureWhiteList]
       SET 
           [DataFine] = @datafine 
     WHERE  idlista in @ids and datafine is null";

 
    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var exec = new
        {
            datafine = _command.DataFine,
            _command.Ids
        };
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlEliminate, exec, transaction);
    }
}