using System.Data;
using DocumentFormat.OpenXml.Math;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence;

public class RelRigheQueryGetByIdPersistence(RelRigheQueryGetById command) : DapperBase, IQuery<IEnumerable<RigheRelDto>?>
{
    private readonly RelRigheQueryGetById _command = command;
    private static readonly string _sqlSelectAll = RelRigheSQLBuilder.SelectAll();
    public async Task<IEnumerable<RigheRelDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var dati = RelTestataKey.Deserialize(_command.IdTestata!);
        var where = string.Empty;
        var idEnte = _command.AuthenticationInfo.IdEnte;

        if (!(dati.TipologiaFattura!.ToLower().Contains("var")
            || dati.TipologiaFattura!.ToLower().Contains("semestrale")
            || dati.TipologiaFattura!.ToLower().Contains("annuale")))
            where += " WHERE r.year=@anno AND r.month=@mese";
        else 
            where += " WHERE r.FlagConguaglio=@FlagConguaglio"; 

        where += " AND r.internal_organization_id=@IdEnte ";
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
            IdContratto = idContratto,
            FlagConguaglio = _command.FlagConguaglio
        };

        return await ((IDatabase)this).SelectAsync<RigheRelDto>(
            connection!,
            sqlEnte,
            query,
            transaction,
            CommandType.Text,
            320);
    }
}