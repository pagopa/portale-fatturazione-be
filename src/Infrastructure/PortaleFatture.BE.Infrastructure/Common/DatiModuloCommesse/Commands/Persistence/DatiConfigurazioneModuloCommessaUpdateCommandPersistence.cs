using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands.Persistence;

public class DatiConfigurazioneModuloCommessaUpdateCommandPersistence : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly DatiConfigurazioneModuloCommessaUpdateCommand _command;

    public DatiConfigurazioneModuloCommessaUpdateCommandPersistence(DatiConfigurazioneModuloCommessaUpdateCommand command)
    => _command = command;

    private static readonly string _sqlUpdateTipo = @"
UPDATE [schema]costonotifiche
SET    datafinevalidita = @dataFineValidita, 
       datamodifica = @dataModifica
WHERE fkprodotto=@prodotto 
    AND fkidtipocontratto=@idtipocontratto 
    AND datainiziovalidita=@datainiziovalidita 
    AND datafinevalidita IS NULL";
    private static readonly string _sqlUpdateCategoria = @"
UPDATE [schema]percentualeanticipo
SET    datafinevalidita = @dataFineValidita, 
       datamodifica = @dataModifica
WHERE fkprodotto=@prodotto 
    AND fkidtipocontratto=@idtipocontratto 
    AND datainiziovalidita=@datainiziovalidita 
    AND datafinevalidita IS NULL";
    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var resultTipi = await ((IDatabase)this).ExecuteAsync(connection!, _sqlUpdateTipo.Add(schema), _command.Tipo, transaction);
        var resultCategorie = await ((IDatabase)this).ExecuteAsync(connection!, _sqlUpdateCategoria.Add(schema), _command.Categoria, transaction);
        return resultTipi + resultCategorie;
    }
}