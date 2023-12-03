using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands.Persistence;

public class DatiModuloCommessaCreateCommandPersistence : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly DatiModuloCommessaCreateListCommand _command; 
    public DatiModuloCommessaCreateCommandPersistence(DatiModuloCommessaCreateListCommand command)
    => _command = command;

    private static readonly string _sqlInsertUpdate = @"
MERGE
INTO         [schema]datimodulocommessa as dm
using        (VALUES
             (
                          @numeronotifichenazionali,
                          @numeronotificheinternazionali,
                          @datamodifica,
                          @idente,
                          @IdTipoContratto,
                          @Prodotto,
                          @IdTipoSpedizione,
                          @annovalidita,
                          @mesevalidita,
                          @datacreazione,
                          @stato,
                          @valoreNazionali,
                          @prezzoNazionali,
                          @valoreInternazionali,
                          @prezzoInternazionali
             )
             ) AS source (numeronotifichenazionali, numeronotificheinternazionali, datamodifica, idente, idtipocontratto, prodotto, idtipospedizione, annovalidita, mesevalidita, datacreazione, stato, valoreNazionali, prezzoNazionali, valoreInternazionali, prezzoInternazionali)
ON           dm.fkidente = source.idente
AND          dm.fkidtipocontratto = source.idtipocontratto
AND          dm.fkprodotto = source.prodotto
AND          dm.fkidtipospedizione = source.idtipospedizione
AND          dm.annovalidita = source.annovalidita
AND          dm.mesevalidita = source.mesevalidita
WHEN matched THEN
UPDATE
SET              numeronotifichenazionali = source.numeronotifichenazionali,
                 numeronotificheinternazionali = source.numeronotificheinternazionali,
                 datamodifica = source.datamodifica,
                 valoreNazionali = source.valoreNazionali,
                 prezzoNazionali = source.prezzoNazionali,
                 valoreInternazionali = source.valoreInternazionali,
                 prezzoInternazionali = source.prezzoInternazionali
WHEN NOT matched THEN
INSERT
       (
              numeronotifichenazionali,
              numeronotificheinternazionali,
              datacreazione,
              fkidente,
              fkidtipocontratto,
              fkidstato,
              fkprodotto,
              fkidtipospedizione,
              annovalidita,
              mesevalidita,
              valoreNazionali,
              prezzoNazionali,
              valoreInternazionali,
              prezzoInternazionali
       )
       VALUES
       (
              source.numeronotifichenazionali,
              source.numeronotificheinternazionali,
              source.datacreazione,
              source.idente,
              source.idtipocontratto,
              source.stato,
              source.prodotto,
              source.idtipospedizione,
              source.annovalidita,
              source.mesevalidita,
              source.valoreNazionali,
              source.prezzoNazionali,
              source.valoreInternazionali,
              source.prezzoInternazionali
       );
";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsertUpdate.Add(schema), _command.DatiModuloCommessaListCommand, transaction); 
    }
} 