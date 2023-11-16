using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands.Persistence;

public sealed class DatiFatturazioneCreateCommandPersistence : DapperBase, ICommand<long>
{
    public bool RequiresTransaction => false;
    private readonly DatiFatturazioneCreateCommand _command;
    public DatiFatturazioneCreateCommandPersistence(DatiFatturazioneCreateCommand command)
        => _command = command;

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
    public async Task<long> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
              => await ((IDatabase)this).ExecuteAsync<long>(connection!, _sqlInsert.Add(schema), _command, transaction);
}