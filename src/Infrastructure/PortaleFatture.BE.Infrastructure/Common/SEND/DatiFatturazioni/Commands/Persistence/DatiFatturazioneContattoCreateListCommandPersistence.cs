using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Commands.Persistence;

public class DatiFatturazioneContattoCreateListCommandPersistence : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly List<DatiFatturazioneContattoCreateCommand> _command;
    public DatiFatturazioneContattoCreateListCommandPersistence(List<DatiFatturazioneContattoCreateCommand> command)
        => _command = command;

    private static readonly string _sqlInsert = @"
        INSERT INTO [schema]DatiFatturazioneContatti
                    (fkiddatifatturazione,
                     email)
        VALUES     (@idDatiFatturazione,
                    @email);";
    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
              => await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert.Add(schema), _command, transaction);
}