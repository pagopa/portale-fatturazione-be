namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

public static class FattureRelExcelBuilder
{
    private static string _sqlNoteSenzaRel = @"
        SELECT [IdFattura]
              ,[RagioneSociale]
              ,[TipoDocumento]
              ,[IdEnte]
              ,[DataFattura]
              ,[Progressivo]
              ,[TotaleFatturaImponibile]
              ,[CodiceMateriale]
              ,[RigaImponibile]
              ,[IdContratto]
              ,[Anno]
              ,[Mese]
              ,@tipologiafattura as TipologiaFattura
              ,[RelTotaleAnalogico]
              ,[RelTotaleDigitale]
              ,[RelTotaleNotificheAnalogiche]
              ,[RelTotaleNotificheDigitali]
              ,[RelTotaleNotifiche]
              ,[RelTotale]
              ,[RelTotaleIvatoAnalogico]
              ,[RelTotaleIvatoDigitale]
              ,[RelTotaleIvato]
              ,[Caricata]
              ,[RelFatturata]
              ,[FkIdTipoContratto]
              ,[TipologiaContratto]
              ,[FatturaInviata]
              ,[RowNum]
              FROM [be].[vwFattureEmesseNoteReport]
              WHERE Anno=@anno and Mese=@mese and TipologiaFattura=@tipologiafattura 
              and CodiceMateriale like '%STORNO%'
              AND [IdEnte] NOT IN
              (
                SELECT rr.internal_organization_id from pfd.RelTestata rr
                WHERE rr.month= @mese and rr.year=@anno and rr.TipologiaFattura=@tipologiafattura) 
                AND (
                @fatturaInviata IS NULL  -- Tutte
                OR (@fatturaInviata = 2 AND FatturaInviata IS NULL)  -- In elaborazione
                OR (FatturaInviata = @fatturaInviata)  -- 0 o 1
              )";

    private static string _sqlNoteSenzaRelSospese = @"
    SELECT [IdFattura]
          ,[RagioneSociale]
          ,[TipoDocumento]
          ,[IdEnte]
          ,[DataFattura]
          ,[Progressivo]
          ,[TotaleFatturaImponibile]
          ,[CodiceMateriale]
          ,[RigaImponibile]
          ,[IdContratto]
          ,[Anno]
          ,[Mese]
          ,[TipologiaFattura]
          ,[RelTotaleAnalogico]
          ,[RelTotaleDigitale]
          ,[RelTotaleNotificheAnalogiche]
          ,[RelTotaleNotificheDigitali]
          ,[RelTotaleNotifiche]
          ,[RelTotale]
          ,[RelTotaleIvatoAnalogico]
          ,[RelTotaleIvatoDigitale]
          ,[RelTotaleIvato]
          ,[Caricata]
          ,[RelFatturata]
          ,[FkIdTipoContratto]
          ,[TipologiaContratto]
          ,[FatturaInviata]
    FROM [pfd].[vwFattureSospeseNoteReport]
    where 
    [Anno]=@anno and 
    [Mese]=@mese and 
    [TipologiaFattura]=@tipologiafattura
    AND IdEnte NOT IN
    (SELECT rr.internal_organization_id from pfd.tmpRelTestata rr
    WHERE rr.month= @mese and rr.year=@anno and rr.TipologiaFattura=@tipologiafattura) 
    AND 
    (
        @FatturaInviata IS NULL  -- Tutte
        OR (@FatturaInviata = 2 AND FatturaInviata IS NULL)  -- In elaborazione
        OR (FatturaInviata = @FatturaInviata)  -- 0 o 1
    ) 
    AND (@FkIdTipoContratto IS NULL OR [FkIdTipoContratto] = @FkIdTipoContratto)
";

    private static string _sqlRel = @"
        SELECT 
        [IdFattura]
        ,[RagioneSociale]
        ,[TipoDocumento]
        ,[IdEnte]
        ,[DataFattura]
        ,[Progressivo]
        ,[TotaleFatturaImponibile]
        ,[CodiceMateriale]
        ,[RigaImponibile]
        ,[IdContratto]
        ,[Anno]
        ,[Mese]
        ,[TipologiaFattura]
        ,[RelTotaleAnalogico]
        ,[RelTotaleDigitale]
        ,[RelTotaleNotificheAnalogiche]
        ,[RelTotaleNotificheDigitali]
        ,[RelTotaleNotifiche]
        ,[RelTotale]
        ,[RelTotaleIvatoAnalogico]
        ,[RelTotaleIvatoDigitale]
        ,[RelTotaleIvato]
        ,[Caricata]
        ,[RelFatturata]
        ,[FkIdTipoContratto]
        ,[TipologiaContratto]
        ,[FatturaInviata]
        ,[RowNum]
        FROM [be].[vwFattureEmesseReport]
        WHERE Mese= @mese and Anno=@anno and TipologiaFattura=@tipologiaFattura  
        AND 
        (
            @fatturaInviata IS NULL  -- Tutte
            OR (@fatturaInviata = 2 AND FatturaInviata IS NULL)  -- In elaborazione
            OR (FatturaInviata = @fatturaInviata)  -- 0 o 1
        ) 
";

    private static string _sqlRelSospese = @"
        SELECT 
           [IdFattura]
          ,[RagioneSociale]
          ,[TipoDocumento]
          ,[IdEnte]
          ,[DataFattura]
          ,[Progressivo]
          ,[TotaleFatturaImponibile]
          ,[CodiceMateriale]
          ,[RigaImponibile]
          ,[IdContratto]
          ,[Anno]
          ,[Mese]
          ,[TipologiaFattura]
          ,[RelTotaleAnalogico]
          ,[RelTotaleDigitale]
          ,[RelTotaleNotificheAnalogiche]
          ,[RelTotaleNotificheDigitali]
          ,[RelTotaleNotifiche]
          ,[RelTotale]
          ,[RelTotaleIvatoAnalogico]
          ,[RelTotaleIvatoDigitale]
          ,[RelTotaleIvato]
          ,[Caricata]
          ,[RelFatturata]
          ,[FkIdTipoContratto]
          ,[TipologiaContratto]
          ,[FatturaInviata]
        FROM [pfd].[vwFattureSospeseReport]
        WHERE Anno=@anno and Mese=@mese and TipologiaFattura=@tipologiafattura  
        AND (
            @FatturaInviata IS NULL  -- Tutte
            OR (@FatturaInviata = 2 AND FatturaInviata IS NULL)  -- In elaborazione
            OR (FatturaInviata = @FatturaInviata)  -- 0 o 1
        )
        AND (@FkIdTipoContratto IS NULL OR [FkIdTipoContratto] = @FkIdTipoContratto)
    ";


    public static string SelectRel()
    {
        return _sqlRel;
    }

    public static string SelectRelSospese()
    {
        return _sqlRelSospese;
    }

    public static string SelectNoteSenzaRel()
    {
        return _sqlNoteSenzaRel;
    }

    public static string SelectNoteSenzaRelSospese()
    {
        return _sqlNoteSenzaRelSospese;
    }

    private static readonly string _orderByRel = @"order by CAST(TotaleFatturaImponibile AS decimal(18,2)) desc";

    public static string OrderByRel()
    {
        return _orderByRel;
    }

    private static readonly string _orderByRelSospese = @"
          ) as a
        order by CAST(
            CASE WHEN a.[TipoDocumento]  = 'TD04'  
            THEN -a.[TotaleFatturaImponibile]
        ELSE a.[TotaleFatturaImponibile]
        END AS decimal(18,2)) desc";

    public static string OrderByRelSospese()
    {
        return _orderByRelSospese;
    }
}