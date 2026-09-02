/****** Oggetto: StoredProcedure [pfd].[EliminaFattura]    Data dello script 31/07/2026 ******/
-- Script autorevole estratto dal DB reale (versione 30/06/2026, v1.0). CREATE OR ALTER per idempotenza
-- (l'originale usa ALTER PROCEDURE, che fallirebbe su un DB fresco). Riprodotta AS-IS: eventuali difetti
-- non si correggono qui (owner: team DB), si allinea la copia seedata quando l'owner rilascia.
--
-- Chiamata da be.spGestioneFattureElimina (EXEC @rc = pfd.EliminaFattura @IdFattura), prosegue se @rc>0.
-- Sposta la fattura in pfd.FattureTestata_Eliminate / FattureRighe_Eliminate, e per le fatture temporanee
-- collegate (via pfd.MesiFatture) in tmpFattureTestata_Eliminate / tmpFattureRighe_Eliminate, poi cancella
-- da FattureTestata/FattureRighe, tmpFattureTestata/Righe, MesiFatture, CreditoSospesoStorico.
--
-- NOTE (difetti reali osservati, NON corretti qui - da segnalare al team DB, non innescati dai test
-- attuali perche' nessun seed ha righe MesiFatture collegate a una fattura eliminanda):
--   * reset sospese: WHERE AnnoRiferimento <> ... AND MeseRiferimento <> ... usa AND al posto di OR:
--     il negato di (anno=A AND mese=M) e' (anno<>A OR mese<>M); una sospesa con stesso anno ma mese
--     diverso non viene ne' spostata ne' resettata.
--   * @tmpFkIdFattura / @tmpFattureSospeseDaResettare sono scalari: se MesiFatture ha piu' righe tmp per
--     la stessa fattura emessa (caso 1:N del primo saldo) viene gestita solo l'ultima.
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [pfd].[EliminaFattura]

    (@IdFattura BIGINT)

AS BEGIN
/* ********************************************************************
Date:            2026-06-30
Version:        1.0
Description:    Eliminazione di una singola fattura

Previous versions:
- 1.0: versione iniziale
******************************************************************** */

SET NOCOUNT ON

--Variabili procedura
DECLARE @FkIdEnte VARCHAR(100);
DECLARE @checkIdFattura BIGINT;

--Variabile per gestione tabelle temporanee - mapping vecchi/nuovi ID tmp
DECLARE @MappingTmp TABLE (
    OldFkIdFatturaTmp BIGINT,
    NewFkIdFatturaTmp BIGINT
);

--Variabili di log
DECLARE @JobId uniqueidentifier = NEWID();
DECLARE @JobName VARCHAR(100) = 'Eliminazione Fattura Singola';
DECLARE @ProcedureName VARCHAR(100) = OBJECT_NAME(@@PROCID);
DECLARE @JobStepNumber VARCHAR(500) = '1';
DECLARE @StartTime DATETIME = GETDATE();
DECLARE @EndTime DATETIME;
DECLARE @ProcedureStatus_Start VARCHAR(10) = 'START';
DECLARE @ProcedureStatus_End VARCHAR(10) = 'COMPLETED';
DECLARE @ProcedureStatus_Error VARCHAR(10) = 'ERROR';


BEGIN TRY

    -- Creo tabella di LOG se non esiste
    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[pfw].[ProceduresLog]') AND type = N'U')
    BEGIN
        CREATE TABLE [pfw].[ProceduresLog](
            [JobId] [uniqueidentifier] NOT NULL,
            [JobName] [varchar](100) NOT NULL,
            [JobStepDescription] [varchar](500) NULL,
            [ProcedureName] [varchar](100) NOT NULL,
            [StartTime] [datetime] NOT NULL,
            [EndTime] [datetime] NULL,
            [Duration] [int] NULL,
            [Status] [varchar](10) NULL,
            [Description] varchar(500) NULL
         CONSTRAINT [PK_JobId_LOG] PRIMARY KEY CLUSTERED
        (
            [JobId] ASC
        )WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]
    END


    --LOG avvio procedura
    INSERT INTO pfw.ProceduresLog (
        JobId, JobName, JobStepDescription, ProcedureName
        ,StartTime, EndTime, Duration, Status, Description)
    VALUES (
        @JobId, @JobName, @JobStepNumber, @ProcedureName
        ,@StartTime, NULL, NULL, @ProcedureStatus_Start, CAST(@IdFattura AS VARCHAR(50)))


    --Se l'input è vuoto o negativo, termino esecuzione
    IF @IdFattura IS NULL OR @IdFattura <= 0
    BEGIN

        SET @EndTime = GETDATE()

        UPDATE pfw.ProceduresLog
        SET EndTime = @EndTime,
            Status = @ProcedureStatus_Error,
            Duration = DATEDIFF(SECOND,  @StartTime, @EndTime),
            Description = 'IdFattura non valido'
        WHERE JobId = @JobId

        -- Termina esecuzione con errore
        SELECT 0 as Result;
        RETURN 0;
    END


    --Verifico esistenza della fattura da Eliminare
    IF NOT EXISTS (
        SELECT 1
        FROM pfd.FattureTestata
        WHERE IdFattura = @IdFattura
            AND
            (FkTipologiaFattura in ('ANTICIPO','ACCONTO')
            -- TODO: rimuovere eccezione per fattura INPS - PRIMO SALDO
            or (FkIdEnte='53b40136-65f2-424b-acfb-7fae17e35c60' and FkTipologiaFattura='PRIMO SALDO')
            )
    )
    BEGIN
        SET @EndTime = GETDATE()

        UPDATE pfw.ProceduresLog
        SET EndTime = @EndTime,
            Status = @ProcedureStatus_Error,
            Duration = DATEDIFF(SECOND,  @StartTime, @EndTime),
            Description = CONCAT('La fattura ',@IdFattura,' non esiste in pfd.FattureTestata')
        WHERE JobId = @JobId

        -- Interrompo procedura con errore
        SELECT 0 as Result;
        RETURN 0;
    END

    --La fattura in input esiste e può essere eliminata

    BEGIN TRANSACTION

    -- Recupero FkIdEnte della fattura (necessario per CreditoSospesoStorico)
    SELECT @FkIdEnte = FkIdEnte
    FROM pfd.FattureTestata
    WHERE IdFattura = @IdFattura


    -- Elimino fattura testata
    INSERT INTO pfd.FattureTestata_Eliminate
          ([IdFattura]
          ,[FkProdotto]
          ,[FkIdTipoDocumento]
          ,[FkTipologiaFattura]
          ,[FkIdEnte]
          ,[FkIdDatiFatturazione]
          ,[DataFattura]
          ,[IdentificativoFattura]
          ,[TotaleFattura]
          ,[Divisa]
          ,[MetodoPagamento]
          ,[AnnoRiferimento]
          ,[MeseRiferimento]
          ,[CausaleFattura]
          ,[Sollecito]
          ,[CodiceContratto]
          ,[SplitPayment]
          ,[Cup]
          ,[Cig]
          ,[IdDocumento]
          ,[DataDocumento]
          ,[NumItem]
          ,[CodCommessa]
          ,[Progressivo]
          ,[Semestre]
          ,[FatturaInviata]
          ,[FaseFatturazione]
          )
    SELECT TOP(1)
           [IdFattura]
          ,[FkProdotto]
          ,[FkIdTipoDocumento]
          ,[FkTipologiaFattura]
          ,[FkIdEnte]
          ,[FkIdDatiFatturazione]
          ,[DataFattura]
          ,[IdentificativoFattura]
          ,[TotaleFattura]
          ,[Divisa]
          ,[MetodoPagamento]
          ,[AnnoRiferimento]
          ,[MeseRiferimento]
          ,[CausaleFattura]
          ,[Sollecito]
          ,[CodiceContratto]
          ,[SplitPayment]
          ,[Cup]
          ,[Cig]
          ,[IdDocumento]
          ,[DataDocumento]
          ,[NumItem]
          ,[CodCommessa]
          ,[Progressivo]
          ,[Semestre]
          ,[FatturaInviata]
          ,[FaseFatturazione]
    FROM pfd.FattureTestata
    WHERE IdFattura = @IdFattura

    --Verifico il record Eliminato (incluso il caso NULL)
    SELECT @checkIdFattura = IdFattura
    FROM pfd.FattureTestata_Eliminate
    WHERE IdFattura = @IdFattura


    --Se non coincide IdFattura, termino esecuzione
    IF ISNULL(@checkIdFattura, -1) <> @IdFattura
    BEGIN
        SET @EndTime = GETDATE()

        UPDATE pfw.ProceduresLog
        SET EndTime = @EndTime,
            Status = @ProcedureStatus_Error,
            Duration = DATEDIFF(SECOND,  @StartTime, @EndTime),
            Description = CONCAT('La fattura: ',@IdFattura,' è stata Eliminata in pfd.FatturaTestata_Eliminate con un identificativo differente: ',@checkIdFattura)
        WHERE JobId = @JobId

        -- Interrompo procedura con errore
        SELECT 0 as Result;
        RETURN 0;
    END


    -- Elimino records FattureRighe
    INSERT INTO pfd.FattureRighe_Eliminate
          ([FkIdFattura]
          ,[NumeroLinea]
          ,[Testo]
          ,[CodiceMateriale]
          ,[Quantita]
          ,[PrezzoUnitario]
          ,[Imponibile]
          ,[RigaBollo]
          ,[PeriodoRiferimento])
    SELECT
           @IdFattura AS [FkIdFattura]
          ,[NumeroLinea]
          ,[Testo]
          ,[CodiceMateriale]
          ,[Quantita]
          ,[PrezzoUnitario]
          ,[Imponibile]
          ,[RigaBollo]
          ,[PeriodoRiferimento]
    FROM pfd.FattureRighe
    WHERE FkIdFattura = @IdFattura;

---------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------

    -- recupero solo gli Id delle fatture temporanee che hanno
    -- il medesimo anno/mese di riferimento della fattura emessa
    DECLARE @tmpFkIdFattura int;

    SELECT @tmpFkIdFattura = mf.FkIdFatturaTmp
    FROM pfd.MesiFatture mf
        JOIN pfd.tmpFattureTestata tft
            ON tft.IdFattura = mf.FkIdFatturaTmp
    WHERE mf.FkIdFattura = @IdFattura
        AND tft.AnnoRiferimento = mf.AnnoRiferimento
        AND tft.MeseRiferimento = mf.MeseRiferimento


    -- Sposto tmpFattureTestata → tmpFattureTestata_Eliminate
    -- Uso MERGE ON 1=0 per forzare sempre INSERT e catturare vecchi/nuovi ID tramite OUTPUT
    -- Nota: FlagFatturata (solo in tmpFattureTestata) non ha colonna corrispondente in _Eliminate
    --       FlagProceduraWhiteList (solo in tmpFattureTestata_Eliminate) assume il valore di default (0)
    MERGE INTO pfd.tmpFattureTestata_Eliminate AS tgt
    USING (
        SELECT
            tt.[IdFattura],
            tt.[FkProdotto],
            tt.[FkIdTipoDocumento],
            tt.[FkTipologiaFattura],
            tt.[FkIdEnte],
            tt.[FkIdDatiFatturazione],
            tt.[DataFattura],
            tt.[IdentificativoFattura],
            tt.[TotaleFattura],
            tt.[Divisa],
            tt.[MetodoPagamento],
            tt.[AnnoRiferimento],
            tt.[MeseRiferimento],
            tt.[CausaleFattura],
            tt.[Sollecito],
            tt.[CodiceContratto],
            tt.[SplitPayment],
            tt.[Cup],
            tt.[Cig],
            tt.[IdDocumento],
            tt.[DataDocumento],
            tt.[NumItem],
            tt.[CodCommessa],
            tt.[Progressivo],
            tt.[FatturaInviata],
            tt.[Semestre]
        FROM pfd.tmpFattureTestata tt
        WHERE tt.IdFattura IN (@tmpFkIdFattura)
    ) AS src ON 1 = 0  -- forza sempre INSERT, mai match
    WHEN NOT MATCHED THEN
        INSERT (
            [FkProdotto], [FkIdTipoDocumento], [FkTipologiaFattura], [FkIdEnte],
            [FkIdDatiFatturazione], [DataFattura], [IdentificativoFattura], [TotaleFattura],
            [Divisa], [MetodoPagamento], [AnnoRiferimento], [MeseRiferimento],
            [CausaleFattura], [Sollecito], [CodiceContratto], [SplitPayment],
            [Cup], [Cig], [IdDocumento], [DataDocumento], [NumItem], [CodCommessa],
            [Progressivo], [FatturaInviata], [Semestre], [IdFattura]
        )
        VALUES (
            src.[FkProdotto], src.[FkIdTipoDocumento], src.[FkTipologiaFattura], src.[FkIdEnte],
            src.[FkIdDatiFatturazione], src.[DataFattura], src.[IdentificativoFattura], src.[TotaleFattura],
            src.[Divisa], src.[MetodoPagamento], src.[AnnoRiferimento], src.[MeseRiferimento],
            src.[CausaleFattura], src.[Sollecito], src.[CodiceContratto], src.[SplitPayment],
            src.[Cup], src.[Cig], src.[IdDocumento], src.[DataDocumento], src.[NumItem], src.[CodCommessa],
            src.[Progressivo], src.[FatturaInviata], src.[Semestre], src.[IdFattura]
        );

    -- Sposto tmpFattureRighe → tmpFattureRighe_Eliminate
    INSERT INTO pfd.tmpFattureRighe_Eliminate
    (   [FkIdFattura],
        [NumeroLinea],
        [Testo],
        [CodiceMateriale],
        [Quantita],
        [PrezzoUnitario],
        [Imponibile],
        [RigaBollo],
        [PeriodoRiferimento]
    )
    SELECT
        tr.[FkIdFattura],
        [NumeroLinea],
        [Testo],
        [CodiceMateriale],
        [Quantita],
        [PrezzoUnitario],
        [Imponibile],
        [RigaBollo],
        [PeriodoRiferimento]
    FROM pfd.tmpFattureRighe tr
    WHERE tr.FkIdFattura = @tmpFkIdFattura


    -- aggiorno flagFatturata sulle fatture sospese collegate alla fattura emessa
    -- ma che hanno diverso anno/mese di riferimento

    DECLARE @tmpFattureSospeseDaResettare int;

    SELECT @tmpFattureSospeseDaResettare = mf.FkIdFatturaTmp
    FROM pfd.MesiFatture mf
        JOIN pfd.tmpFattureTestata tft
            ON tft.IdFattura = mf.FkIdFatturaTmp
    WHERE mf.FkIdFattura = @IdFattura
        AND tft.AnnoRiferimento <> mf.AnnoRiferimento
        AND tft.MeseRiferimento <> mf.MeseRiferimento


    UPDATE pfd.tmpFattureTestata
    SET FlagFatturata = 0
    WHERE IdFattura = @tmpFattureSospeseDaResettare

---------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------

    --Elimino record fattura_righe
    DELETE FROM pfd.FattureRighe WHERE FkIdFattura = @IdFattura;

    --Elimino record fattura_testata
    DELETE FROM pfd.FattureTestata WHERE IdFattura = @IdFattura;

    -- DELETE da tmpFattureRighe
    DELETE tr
    FROM pfd.tmpFattureRighe tr
    WHERE tr.FkIdFattura = @tmpFkIdFattura;

    -- DELETE da tmpFattureTestata
    DELETE tt
    FROM pfd.tmpFattureTestata tt
    WHERE tt.IdFattura = @tmpFkIdFattura;

    -- DELETE da MesiFatture
    DELETE mf
    FROM pfd.MesiFatture mf
    WHERE FkIdFattura = @IdFattura

    -- DELETE da CreditoSospesoStorico
    DELETE css
    FROM pfd.CreditoSospesoStorico css
    WHERE FkIdFattura = @IdFattura

    --LOG completamento stored procedure
    SET @EndTime = GETDATE()

    UPDATE pfw.ProceduresLog
    SET EndTime = @EndTime,
        Status = @ProcedureStatus_End,
        Duration = DATEDIFF(SECOND,  @StartTime, @EndTime)
    WHERE JobId = @JobId


    COMMIT TRANSACTION

    -- Procedura eseguita correttamente
    SELECT 1 as Result;
    RETURN 1;

END TRY

BEGIN CATCH


    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;

        --LOG Errore e rollback
        SET @EndTime = GETDATE()

        UPDATE pfw.ProceduresLog
        SET EndTime = @EndTime,
            Status = @ProcedureStatus_Error,
            Duration = DATEDIFF(SECOND,  @StartTime, @EndTime),
            Description = ERROR_MESSAGE()
        WHERE JobId = @JobId
    END

    -- Procedura terminata con errori
    SELECT 0 as Result;
    RETURN 0;

END CATCH

END
GO
