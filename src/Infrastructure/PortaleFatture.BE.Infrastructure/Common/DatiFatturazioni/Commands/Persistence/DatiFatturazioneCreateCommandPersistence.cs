using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands.Persistence;

public sealed class DatiFatturazioneCreateCommandPersistence(DatiFatturazioneCreateCommand command) : DapperBase, ICommand<long?>
{
    public bool RequiresTransaction => true;
    private readonly DatiFatturazioneCreateCommand _command = command;

    private static readonly string _sqlInsert = @"
INSERT INTO [schema]DatiFatturazione
        (cup,
        cig,
        codcommessa,
        datadocumento,
        splitpayment,
        fkidente,
        iddocumento,
        datacreazione, 
        [map],
        fktipocommessa,
        pec,
        fkprodotto)
VALUES (@cup, 
        @cig,
        @codcommessa,
        @datadocumento,
        @splitpayment,
        @idente, 
        @iddocumento,
        @datacreazione,
        @map, 
        @tipocommessa,
        @pec,   
        @prodotto);
Select SCOPE_IDENTITY() 'SCOPE_IDENTITY'";
    public async Task<long?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
              => await ((IDatabase)this).ExecuteAsync<long?>(connection!, _sqlInsert.Add(schema), new
              { 
                  cup = _command.Cup,
                  cig = _command.Cig,
                  codCommessa = _command.CodCommessa,
                  dataDocumento = _command.DataDocumento,
                  splitPayment = _command.SplitPayment,
                  idEnte = _command.AuthenticationInfo.IdEnte,
                  idDocumento = _command.IdDocumento,
                  dataCreazione = _command.DataCreazione,
                  map = _command.Map,
                  tipoCommessa = _command.TipoCommessa,
                  pec = _command.Pec, 
                  prodotto = _command.AuthenticationInfo.Prodotto
              }, transaction);
}