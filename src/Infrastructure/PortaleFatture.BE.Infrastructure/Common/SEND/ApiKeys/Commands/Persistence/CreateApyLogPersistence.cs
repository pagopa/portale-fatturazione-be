using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands.Persistence;

public class CreateApyLogPersistence(CreateApyLogCommand command) : DapperBase, ICommand<int?>
{
    private readonly CreateApyLogCommand _command = command;
    private static readonly string _sqlInsert =
        $@"
INSERT INTO [pfw].[ApiLog]
           ([Id]
           ,[FkIdEnte]
           ,[Timestamp]
           ,[FunctionName]
           ,[Method]
           ,[Stage]
           ,[Payload]
           ,[IpAddress]
           ,[Uri])
     VALUES
           (@Id
           ,@FkIdEnte
           ,@Timestamp
           ,@FunctionName
           ,@Method
           ,@Stage
           ,@Payload
           ,@IpAddress
           ,@Uri)
";

    public bool RequiresTransaction => false;

    public async Task<int?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert, _command, transaction); 
    }
}