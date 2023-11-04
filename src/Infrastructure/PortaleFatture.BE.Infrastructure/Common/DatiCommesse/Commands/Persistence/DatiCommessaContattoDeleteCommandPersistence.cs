using PortaleFatture.BE.Infrastructure.Common.Persistence;
using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Commands.Persistence; 
public class DatiCommessaContattoDeleteCommandPersistence : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly DatiCommessaContattoDeleteCommand _command;
    public DatiCommessaContattoDeleteCommandPersistence(DatiCommessaContattoDeleteCommand command)
        => _command = command;

    private static readonly string _sqlDelete = @"
DELETE FROM pfw.daticommessacontatti
WHERE  FkIdDatiCommessa = @IdDatiCommessa;";
    public async Task<int> Execute(IDbConnection? connection, IDbTransaction? transaction, CancellationToken cancellationToken = default)
              => await ((IDatabase)this).ExecuteAsync(connection!, _sqlDelete, _command, transaction);
}