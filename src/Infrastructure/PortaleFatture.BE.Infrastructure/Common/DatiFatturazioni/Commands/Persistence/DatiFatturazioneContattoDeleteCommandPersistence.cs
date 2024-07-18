using PortaleFatture.BE.Infrastructure.Common.Persistence;
using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands.Persistence; 
public class DatiFatturazioneContattoDeleteCommandPersistence : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly DatiFatturazioneContattoDeleteCommand _command;
    public DatiFatturazioneContattoDeleteCommandPersistence(DatiFatturazioneContattoDeleteCommand command)
        => _command = command;

    private static readonly string _sqlDelete = @"
DELETE FROM [schema]DatiFatturazioneContatti
WHERE  FkIdDatiFatturazione = @IdDatiFatturazione;";
    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
              => await ((IDatabase)this).ExecuteAsync(connection!, _sqlDelete.Add(schema), _command, transaction);
}