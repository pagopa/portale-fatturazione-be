using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;

public class StoricoCreateCommandPersistence : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly StoricoCreateCommand _command;
    public StoricoCreateCommandPersistence(StoricoCreateCommand command)
    => _command = command;

    private static readonly string _sqlInsert = @" 
INSERT INTO [schema][Log]
           ([FkIdEnte]
           ,[IdUtente]
           ,[DataEvento]
           ,[DescrizioneEvento]
           ,[JsonTransazione])
     VALUES
           (@IdEnte,
            @IdUtente, 
            @DataEvento,
            @DescrizioneEvento,
            @JsonTransazione)
";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert.Add(schema), new 
        {
            idEnte = _command.AuthenticationInfo.IdEnte,
            idUtente = _command.AuthenticationInfo.Id,
            dataEvento = _command.DataEvento,
            descrizioneEvento = _command.DescrizioneEvento,
            jsonTransazione = _command.JsonTransazione 
        }, transaction);
    }
} 