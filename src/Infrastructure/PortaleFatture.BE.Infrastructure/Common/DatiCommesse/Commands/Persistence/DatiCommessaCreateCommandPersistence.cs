using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Commands.Persistence;

public sealed class DatiCommessaCreateCommandPersistence : DapperBase, ICommand<long>
{
    public bool RequiresTransaction => false;
    private readonly DatiCommessaCreateCommand _command;
    public DatiCommessaCreateCommandPersistence(DatiCommessaCreateCommand command)
        => _command = command;

    private static readonly string _sqlInsert = @"
INSERT INTO pfw.daticommessa
        (cup,
        cig,
        codcommessa,
        datadocumento,
        splitpayment,
        fkidente,
        fkidtipocontratto,
        iddocumento,
        datacreazione,
        [map],
        flagordinecontratto)
VALUES (@cup, 
        @cig,
        @codcommessa,
        @datadocumento,
        @splitpayment,
        @idente,
        @idtipocontratto,
        @iddocumento,
        @datacreazione,
        @map,
        @flagordinecontratto);
Select SCOPE_IDENTITY() 'SCOPE_IDENTITY'";
    public async Task<long> Execute(IDbConnection? connection, IDbTransaction? transaction, CancellationToken cancellationToken = default)
              => await ((IDatabase)this).ExecuteAsync<long>(connection!, _sqlInsert, _command, transaction);
}