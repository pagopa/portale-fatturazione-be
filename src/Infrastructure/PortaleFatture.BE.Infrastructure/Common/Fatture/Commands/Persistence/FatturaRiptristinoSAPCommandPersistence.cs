using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Commands.Persistence;

public class FatturaRiptristinoSAPCommandPersistence(FatturaRiptristinoSAPCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly FatturaRiptristinoSAPCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;  

    private static readonly string _sql   = @"
UPDATE [pfd].[FattureTestata]
   SET  
       [FatturaInviata] = @FatturaInviata
 WHERE AnnoRiferimento=@anno and MeseRiferimento=@mese and FkTipologiaFattura=@TipologiaFattura AND [FatturaInviata] = @StatoAtteso"; 
 
    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        return await ((IDatabase)this).ExecuteAsync(connection!, _sql.Add(schema), _command, transaction);
    }
}