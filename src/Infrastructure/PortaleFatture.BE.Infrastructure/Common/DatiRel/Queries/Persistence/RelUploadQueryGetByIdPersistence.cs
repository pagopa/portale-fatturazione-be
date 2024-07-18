using System.Data;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence;

public class RelUploadQueryGetByIdPersistence(RelUploadGetById command) : DapperBase, IQuery<IEnumerable<RelUpload>?>
{
    private readonly RelUploadGetById _command = command;
    private static readonly string _sqlSelectAll = RelUploadSQLBuilder.SelectAll();
    public async Task<IEnumerable<RelUpload>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var where = string.Empty;
        var idEnte = _command.AuthenticationInfo.IdEnte;
        where += " WHERE FkIdEnte=@IdEnte ";
        var anno = _command.Anno;
        var mese = _command.Mese;
        var tipoFattura = _command.TipologiaFattura;
        var idContratto = _command.IdContratto;
        var azione = _command.Azione;

        where += " AND TipologiaFattura=@TipologiaFattura";
        where += " AND year=@anno";
        where += " AND month=@mese";
        where += " AND contract_id=@IdContratto"; 
        where += " AND azione=@azione";

        var sqlEnte = _sqlSelectAll;
        sqlEnte += where;
        sqlEnte += " ORDER BY DataEvento DESC";
        sqlEnte = sqlEnte.Add(schema);

        var query = new RelQueryDto
        {
            Anno = anno,
            Mese = mese,
            IdEnte = idEnte,
            TipologiaFattura = tipoFattura,
            IdContratto = idContratto,
            Azione = azione,
            Size = 10
        };

        return await ((IDatabase)this).SelectAsync<RelUpload>(
            connection!,
            sqlEnte,
            query,
            transaction);
    }
}