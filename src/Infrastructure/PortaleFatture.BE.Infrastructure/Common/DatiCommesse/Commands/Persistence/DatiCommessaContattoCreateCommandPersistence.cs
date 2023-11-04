using PortaleFatture.BE.Infrastructure.Common.Persistence;
using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Commands.Persistence;

public class DatiCommessaContattoCreateCommandPersistence : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly DatiCommessaContattoCreateCommand _command;
    public DatiCommessaContattoCreateCommandPersistence(DatiCommessaContattoCreateCommand command)
        => _command = command;

    private static readonly string _sqlInsert = @"
        INSERT INTO pfw.daticommessacontatti
                    (fkiddaticommessa,
                     email,
                     tipo)
        VALUES     (@iddaticommessa,
                    @email,
                    @tipo);";
    public async Task<int> Execute(IDbConnection? connection, IDbTransaction? transaction, CancellationToken cancellationToken = default)
              => await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert, _command, transaction);
} 