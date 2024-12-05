using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence.Builder;

public class EnteSQLBuilder
{
    private static string _sql = @"
SELECT e.[InternalIstitutionId] as IdEnte
      ,[description] as RagioneSociale
	  , c.FkIdTipoContratto as IdTipoContratto
	  , t.Descrizione as TipoContratto
	  , c.onboardingtokenid as IdContratto
	  , c.product as Prodotto
	  , ISNULL(originIdPadre, e.originId) as CodiceIPA
      , c.codiceSDI as codiceSDI
	  , e.institutionType as institutionType 
  FROM [pfd].[Enti] e
  inner join pfd.contratti c
  left join pfw.TipoContratto t
  ON t.IdTipoContratto = c.FkIdTipoContratto
  on e.InternalIstitutionId = c.internalistitutionid
";
    private static string WhereById()
    {
        Ente? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<Ente>();
        return $"{fieldIdEnte} = @{nameof(@obj.IdEnte)}";
    }

    private static SqlBuilder CreateSelect()
    {
        Ente? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.IdEnte).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Profilo).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Descrizione).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Email).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Address).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Cap).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.CodiceIstat).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Citta).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Provincia).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.Nazione).GetAsColumn<Ente>());
        builder.Select(nameof(@obj.PartitaIva).GetAsColumn<Ente>()); 
        return builder;
    }

    public static string SelectByIdEnte()
    {
        var tableName = nameof(Ente);
        tableName = tableName.GetTable<Ente>();
        var builder = CreateSelect();
        var where = WhereById();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }

    public static string SelectContrattoByIdEnte()
    {
        return _sql;
    }
}
