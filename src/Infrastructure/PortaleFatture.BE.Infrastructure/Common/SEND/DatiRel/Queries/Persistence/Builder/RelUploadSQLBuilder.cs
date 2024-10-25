namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence.Builder;

public static class RelUploadSQLBuilder
{
    private static string _sql = @" 
    SELECT TOP 10 [FkIdEnte] as idEnte
        ,[contract_id] as idContratto
        ,[TipologiaFattura]
        ,[year] as anno
        ,[month] as mese
        ,[DataEvento]
        ,[IdUtente]
        ,[Azione]
        ,[Hash]
	    ,description as RagioneSociale
    FROM [schema][RelUpload] u
    inner join [schema]Enti e
    on e.InternalIstitutionId = u.FkIdEnte
    ";

    public static string SelectAll()
    {
        return _sql;
    }
}