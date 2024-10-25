using System.Data;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence;

public class RelTestataQueryGetByIdPersistence(RelTestataQueryGetById command) : DapperBase, IQuery<RelTestataDettaglioDto?>
{
    private readonly RelTestataQueryGetById _command = command;
    private static readonly string _sqlSelectAll = RelTestataSQLBuilder.SelectDettaglio();
    public async Task<RelTestataDettaglioDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var dati = RelTestataKey.Deserialize(_command.IdTestata!);
        var where = string.Empty;
        var idEnte = _command.AuthenticationInfo.IdEnte;
        where += " WHERE internal_organization_id=@IdEnte ";
        var anno = dati.Anno;
        var mese = dati.Mese;
        var tipoFattura = dati.TipologiaFattura;
        var idContratto = dati.IdContratto;

        if (_command.AuthenticationInfo.Auth == AuthType.SELFCARE)
        {
            if (idEnte != dati.IdEnte)
                throw new DomainException("");
        }
        else
            idEnte = dati.IdEnte;

        if (!string.IsNullOrEmpty(tipoFattura))
            where += " AND TipologiaFattura=@TipologiaFattura";
        else
            throw new DomainException("");

        where += " AND year=@anno";
        where += " AND month=@mese";

        if (!string.IsNullOrEmpty(idContratto))
            where += " AND contract_id=@IdContratto";
        else
            throw new DomainException("");

        var sqlEnte = _sqlSelectAll;
        sqlEnte += where;

        var query = new RelQueryDto
        {
            Anno = anno,
            Mese = mese,
            IdEnte = idEnte,
            TipologiaFattura = tipoFattura,
            IdContratto = idContratto
        };

        return await ((IDatabase)this).SingleAsync<RelTestataDettaglioDto>(
            connection!,
            sqlEnte,
            query,
            transaction);
    }
}