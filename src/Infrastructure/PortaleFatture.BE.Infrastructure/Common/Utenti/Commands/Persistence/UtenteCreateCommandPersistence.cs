using System.Data;
using PortaleFatture.BE.Core.Entities.Utenti;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Utenti.Commands.Persistence;

public class UtenteCreateCommandPersistence : DapperBase, ICommand<Utente?>
{
    public bool RequiresTransaction => false;
    private readonly UtenteCreateCommand _command;
    public UtenteCreateCommandPersistence(UtenteCreateCommand command)
    => _command = command;

    private static readonly string _sqlInsertUpdate = @"
MERGE
INTO         [schema]utenti as dm
using        (VALUES
             (
                          @idEnte,
                          @idUtente, 
                          @dataPrimo,
                          @dataUltimo 
             )
             ) AS source (idEnte, idUtente, dataPrimo, dataUltimo)
ON           dm.fkidente = source.idente
AND          dm.idUtente = source.idUtente 
WHEN matched THEN
UPDATE
SET          dataUltimo = source.dataUltimo 
WHEN NOT matched THEN
INSERT
       (
              fkidente,
              idUtente,
              dataPrimo,
              dataUltimo 
       )
       VALUES
       (
              source.idEnte,
              source.idUtente,
              source.dataPrimo,
              source.dataUltimo 
       );
SELECT * FROM  [schema]utenti WHERE fkidente=@idEnte AND @idUtente = idUtente;
";

    public async Task<Utente?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var utente = await ((IDatabase)this).ExecuteAsync<Utente>(connection!, _sqlInsertUpdate.Add(schema),
            new { idEnte = _command.AuthenticationInfo!.IdEnte, 
                idUtente = _command.AuthenticationInfo.Id, 
                dataPrimo = _command.DataPrimo, 
                dataUltimo = _command.DataUltimo }, transaction);
        
        return utente;
    }
}