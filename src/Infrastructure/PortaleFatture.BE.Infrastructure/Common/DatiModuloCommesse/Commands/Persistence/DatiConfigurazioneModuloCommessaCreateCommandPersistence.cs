using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands.Persistence;

public class DatiConfigurazioneModuloCommessaCreateCommandPersistence : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly DatiConfigurazioneModuloCommessaCreateCommand _command;

    public DatiConfigurazioneModuloCommessaCreateCommandPersistence(DatiConfigurazioneModuloCommessaCreateCommand command)
    => _command = command;

    private static readonly string _sqlInsertTipo = @"
INSERT INTO [schema]costonotifiche
           (MediaNotificaNazionale,
            MediaNotificaInternazionale,
            FkIdTipoSpedizione,
            FkIdTipoContratto,
            FkProdotto,
            DataInizioValidita, 
            DataCreazione, 
            Descrizione)
VALUES     (@medianotificanazionale,
            @medianotificainternazionale, 
            @idtipospedizione,
            @idtipocontratto,
            @prodotto,
            @datainiziovalidita,
            @datacreazione,
            @descrizione)";

    private static readonly string _sqlInsertCategoria = @"
INSERT INTO [schema]percentualeanticipo
            (fkprodotto,
             fkidtipocontratto,
             fkidcategoriaspedizione,
             percentuale,
             descrizione,
             datainiziovalidita, 
             datacreazione)
VALUES     (@prodotto,
            @idtipocontratto,
            @idcategoriaspedizione,
            @percentuale,
            @descrizione,
            @datainiziovalidita, 
            @datacreazione); ";
    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            var resultTipi = await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsertTipo.Add(schema), _command.Tipi, transaction);
            var resultCategorie = await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsertCategoria.Add(schema), _command.Categorie, transaction);
            return resultTipi + resultCategorie;
        }
        catch (Exception ex)
        {
            var dummy = ex;
            return 0;
        }
 
    }
}