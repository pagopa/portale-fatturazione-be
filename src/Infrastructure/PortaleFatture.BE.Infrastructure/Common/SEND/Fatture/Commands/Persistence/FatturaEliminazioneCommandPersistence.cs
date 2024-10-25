using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

public class FatturaEliminazioneCommandPersistence(FatturaCancellazioneCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly FatturaCancellazioneCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlEliminate = @"
DELETE FROM [pfd].[FattureRighe_Eliminate] 
WHERE FKIdFattura IN @IdFatture;
DELETE FROM [pfd].[FattureTestata_Eliminate] 
WHERE IdFattura IN @IdFatture";

    private static readonly string _sql = @"
DELETE FROM [pfd].[FattureRighe] 
WHERE FKIdFattura IN @IdFatture;
DELETE FROM [pfd].[FattureTestata] 
WHERE IdFattura IN @IdFatture";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var sql = _command.Cancellazione == true ? _sql : _sqlEliminate;
        return await ((IDatabase)this).ExecuteAsync(connection!, sql.Add(schema), _command, transaction);
    }
}