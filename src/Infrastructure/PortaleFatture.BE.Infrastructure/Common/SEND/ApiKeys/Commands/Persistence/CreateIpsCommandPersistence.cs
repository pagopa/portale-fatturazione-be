using System.Data;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands.Persistence;

public class CreateIpsCommandPersistence(CreateIpsCommand command) : DapperBase, IQuery<int?>
{
    private readonly CreateIpsCommand _command = command;
    private static readonly string _sqlInsert =
        $@"
INSERT INTO [pfw].[ApiKeysIPs]
           ([FkIdEnte]
           ,[DataCreazione]
           ,[IPAddress])
     VALUES
           (@FkIdEnte
           ,@DataCreazione
           ,@IPAddress) 
";

    public async Task<int?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert, _command, transaction);
        }
        catch (SqlException ex) when (ex.Number == 2601)  
        {
            return -1;
        }
        catch  
        {
            return -1;
        }  
    }
} 