using System.Data;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands.Persistence;

public class DatiFatturazioneUpdateCommandPersistence : DapperBase, ICommand<DatiFatturazione?>
{
    public bool RequiresTransaction => false;
    private readonly DatiFatturazioneUpdateCommand _command;
    public DatiFatturazioneUpdateCommandPersistence(DatiFatturazioneUpdateCommand command)
        => _command = command;
    private static readonly string _sqlUpdate = @"
UPDATE [schema]DatiFatturazione
SET    cup = @cup,
       cig = @cig,
       codcommessa = @codcommessa,
       datadocumento = @datadocumento,
       splitpayment = @splitpayment, 
       iddocumento = @iddocumento, 
       datamodifica = @datamodifica,
       [map] = @map,
       fktipocommessa = @tipocommessa,
       pec = @pec,
       fkprodotto = @prodotto
WHERE  IdDatiFatturazione = @id;
SELECT *, IdDatiFatturazione as id, fktipocommessa as tipocommessa, fkidEnte as idente, fkprodotto as prodotto FROM pfw.DatiFatturazione WHERE IdDatiFatturazione = @id;";
    public async Task<DatiFatturazione?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
              => await ((IDatabase)this).ExecuteAsync<DatiFatturazione>(connection!, _sqlUpdate.Add(schema), _command, transaction);
}
