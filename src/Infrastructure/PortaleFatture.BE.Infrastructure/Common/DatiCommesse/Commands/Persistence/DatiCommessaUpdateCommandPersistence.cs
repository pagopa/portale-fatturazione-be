using System.Data;
using PortaleFatture.BE.Core.Entities.DatiCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Commands.Persistence;

public class DatiCommessaUpdateCommandPersistence : DapperBase, ICommand<DatiCommessa?>
{
    public bool RequiresTransaction => false;
    private readonly DatiCommessaUpdateCommand _command;
    public DatiCommessaUpdateCommandPersistence(DatiCommessaUpdateCommand command)
        => _command = command;
    private static readonly string _sqlUpdate = @"
UPDATE pfw.daticommessa
SET    cup = @cup,
       cig = @cig,
       codcommessa = @codcommessa,
       datadocumento = @datadocumento,
       splitpayment = @splitpayment, 
       fkidtipocontratto = @idtipocontratto,
       iddocumento = @iddocumento, 
       datamodifica = @datamodifica,
       [map] = @map,
       flagordinecontratto = @flagordinecontratto
WHERE  iddaticommessa = @id;
SELECT *, iddaticommessa as id, fkidtipocontratto as idtipocontratto, fkidEnte as idente FROM pfw.daticommessa WHERE iddaticommessa = @id;";
    public async Task<DatiCommessa?> Execute(IDbConnection? connection, IDbTransaction? transaction, CancellationToken cancellationToken = default)
              => await ((IDatabase)this).ExecuteAsync<DatiCommessa>(connection!, _sqlUpdate, _command, transaction);
}
