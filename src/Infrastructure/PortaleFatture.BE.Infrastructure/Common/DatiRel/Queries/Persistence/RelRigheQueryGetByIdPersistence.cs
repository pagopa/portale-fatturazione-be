using System.Data;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence;

public class RelRigheQueryGetByIdPersistence(RelRigheQueryGetById command) : DapperBase, IQuery<IEnumerable<RigheRelDto>?>
{
    private readonly RelRigheQueryGetById _command = command;
    private static readonly string _sqlSelectAll = RelRigheSQLBuilder.SelectAll();
    public async Task<IEnumerable<RigheRelDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var dati = RelTestataKey.Deserialize(_command.IdTestata!);
        var where = string.Empty;
        var idEnte = _command.AuthenticationInfo.IdEnte;
        where += " WHERE r.internal_organization_id=@IdEnte ";
        var anno = dati.Anno;
        var mese = dati.Mese;
        var tipoFattura = dati.TipologiaFattura;
        var idContratto = dati.IdContratto;

        if (idEnte != dati.IdEnte)
            throw new DomainException("");

        if (!string.IsNullOrEmpty(tipoFattura))
            if (tipoFattura == TipologiaFattura.PRIMOSALDO)
                where += $" AND (TipologiaFattura=@TipologiaFattura OR TipologiaFattura='{TipologiaFattura.ASSEVERAZIONE}')";
            else
                where += " AND TipologiaFattura=@TipologiaFattura";
        else
            throw new DomainException("");

        where += " AND r.year=@anno";
        where += " AND r.month=@mese";

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

        return await ((IDatabase)this).SelectAsync<RigheRelDto>(
            connection!,
            sqlEnte,
            query,
            transaction);
    }
}