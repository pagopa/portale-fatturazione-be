using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands.Persistence;

public class DatiModuloCommessaCreateTotaleCommandPersistence : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly DatiModuloCommessaTotaleCreateListCommand _command; 
    public DatiModuloCommessaCreateTotaleCommandPersistence(DatiModuloCommessaTotaleCreateListCommand command)
    => _command = command;

    private static readonly string _sqlInsertUpdate = @"
MERGE
INTO         [schema]DatiModuloCommessaTotali as dm
using        (VALUES
             (
                @idente,
                @IdTipoContratto,
                @Prodotto,
                @IdCategoriaSpedizione,
                @annovalidita,
                @mesevalidita,
                @TotaleCategoria,
                @stato
             )
             ) AS source (idente, idtipocontratto, prodotto, IdCategoriaSpedizione, annovalidita, mesevalidita, TotaleCategoria, stato )
ON           dm.fkidente = source.idente
AND          dm.fkidtipocontratto = source.idtipocontratto
AND          dm.fkprodotto = source.prodotto
AND          dm.fkIdCategoriaSpedizione = source.IdCategoriaSpedizione
AND          dm.annovalidita = source.annovalidita
AND          dm.mesevalidita = source.mesevalidita
WHEN matched THEN
UPDATE
SET              TotaleCategoria = source.TotaleCategoria
WHEN NOT matched THEN
INSERT
       (
              fkidente,
              fkidtipocontratto,
              fkidstato,
              fkprodotto,
              fkIdCategoriaSpedizione,
              annovalidita,
              mesevalidita,
              TotaleCategoria
       )
       VALUES
       (
              source.idente,
              source.idtipocontratto,
              source.stato,
              source.prodotto,
              source.IdCategoriaSpedizione,
              source.annovalidita,
              source.mesevalidita,
              source.TotaleCategoria
       );
";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsertUpdate.Add(schema), _command.DatiModuloCommessaTotaleListCommand, transaction); 
    }
} 