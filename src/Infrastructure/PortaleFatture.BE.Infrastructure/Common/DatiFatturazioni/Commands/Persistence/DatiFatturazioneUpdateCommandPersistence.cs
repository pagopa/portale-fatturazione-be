using System.Data;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands.Persistence;

public class DatiFatturazioneUpdateCommandPersistence(DatiFatturazioneUpdateCommand command) : DapperBase, ICommand<DatiFatturazione?>
{
    public bool RequiresTransaction => true;
    private readonly DatiFatturazioneUpdateCommand _command = command;
    private static readonly string _sqlUpdate = @"
UPDATE [schema]DatiFatturazione
SET    cup = @cup,
       notaLegale = @notaLegale,
       codcommessa = @codcommessa,
       datadocumento = @datadocumento,
       splitpayment = @splitpayment, 
       iddocumento = @iddocumento, 
       datamodifica = @datamodifica,
       [map] = @map,
       fktipocommessa = @tipocommessa,
       pec = @pec
WHERE  IdDatiFatturazione = @id;";

    private static readonly string _sqlSelect = DatiFatturazioneSQLBuilder.SelectById();


    public async Task<DatiFatturazione?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        try
        {
            var rowAffected = await ((IDatabase)this).ExecuteAsync(connection!, _sqlUpdate.Add(schema), new
            {
                id = _command.Id,
                cup = _command.Cup,
                notaLegale = _command.NotaLegale,
                codCommessa = _command.CodCommessa,
                dataDocumento = _command.DataDocumento,
                splitPayment = _command.SplitPayment,
                idDocumento = _command.IdDocumento,
                map = _command.Map,
                tipoCommessa = _command.TipoCommessa,
                pec = _command.Pec,
                dataModifica = _command.DataModifica
            }, transaction);

            if (rowAffected == 1)
                return await ((IDatabase)this).SingleAsync<DatiFatturazione>(connection!, _sqlSelect.Add(schema), new { id = _command.Id }, transaction);
        }
        catch { }
        return null;
    }

}