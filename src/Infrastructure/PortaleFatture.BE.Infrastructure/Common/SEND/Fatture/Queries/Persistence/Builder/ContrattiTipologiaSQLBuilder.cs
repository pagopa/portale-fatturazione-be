namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

internal static class ContrattiTipologiaSQLBuilder
{
    private static string _sqlCount = @"
SELECT Count(*)
FROM   [pfd].[contrattitipologia] ct
       RIGHT JOIN pfd.enti e
               ON ct.fkidente = e.internalistitutionid
       LEFT OUTER JOIN pfd.contratti c
                    ON e.internalistitutionid = c.internalistitutionid 
";


    private static string _sql = @"
SELECT e.internalistitutionid AS IdEnte,
       description            AS RagioneSociale,
       e.lastmodified         AS UltimaModificaContratto,
       c.fkidtipocontratto    AS TipoContratto,
       c.onboardingtokenid    AS IdContratto,
       ct.datainserimento,
	   tp.Descrizione         AS DescrizioneTipoContratto
FROM   [pfd].[contrattitipologia] ct
       RIGHT JOIN pfd.enti e
               ON ct.fkidente = e.internalistitutionid
       LEFT OUTER JOIN pfd.contratti c
                    ON e.internalistitutionid = c.internalistitutionid 
       LEFT OUTER JOIN pfw.TipoContratto tp
	           ON tp.IdTipoContratto = c.FkIdTipoContratto
";


    private static string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";
    public static string OffSet()
    {
        return _offSet;
    }

    public static string OrderBy()
    {
        return " ORDER BY RagioneSociale ";
    }

    public static string SelectAll()
    {
        return _sql;
    }

    public static string SelectAllCount()
    {
        return _sqlCount;
    }

    private static string _selectTipologiaContratto = @$"
    select distinct(FkIdTipoContratto) as id, t.Descrizione as descrizione from pfd.Contratti c
    inner join pfw.TipoContratto t
    on t.IdTipoContratto = c.FkIdTipoContratto 
";
    public static string SelectTipologiaContratto()
    {
        return _selectTipologiaContratto;
    } 
}

