/****** Oggetto: StoredProcedure [be].[spGestioneFatturePosticipa]    Data dello script 23/07/2026 15:23:19 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO






-- =============================================
/*
  Data creazione:        29/06/2026
  Data ultima modifica:  29/06/2026
  Descrizione:           Creazione fattura posticipata
  Target utilizzo:       Pulsante Posticipa della pagina PF "GestioneFatture" / "DocumentiEmessi"
  Versione:              1.0
*/
-- =============================================
CREATE PROCEDURE [be].[spGestioneFatturePosticipa]
(
    -- parameters for the stored procedure
    @IdFattura int null,
	@Anno int, 
	@Mese int,
	@IdEnte uniqueidentifier,
	@TipologiaFattura nvarchar(50),
    @IdUtente nvarchar(50),
	@Note json
)
AS
BEGIN
    SET NOCOUNT ON

    --Variabili di log
    DECLARE @JobId uniqueidentifier = NEWID();
    DECLARE @JobName VARCHAR(100) = 'POSTICIPA FATTURA';
    DECLARE @ProcedureName VARCHAR(100) = OBJECT_NAME(@@PROCID);
    DECLARE @JobStepNumber VARCHAR(500) = '1';
    DECLARE @StartTime DATETIME = GETDATE();
    DECLARE @EndTime DATETIME;
    DECLARE @ProcedureStatus_Start VARCHAR(10) = 'START';
    DECLARE @ProcedureStatus_End VARCHAR(10) = 'COMPLETED';
    DECLARE @ProcedureStatus_Error VARCHAR(10) = 'ERROR';

	DECLARE @stringFattura nvarchar(100) = CONCAT(@IdFattura,' ', @Anno,' ', @Mese,' ', @TipologiaFattura,' ', @IdEnte)

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

		-- Creo variabile tabella per GestioneFatture
		DECLARE @tmpGestioneFatture TABLE (
			[FkIdFattura] int,
    		[FkIdEnte] [nvarchar](50),
			[FkTipologiaFattura] [nvarchar](50),
			[Anno] [int],
			[Mese] [int],
			[DataInserimento] [datetime],
			[DataCancellazione] [datetime],
			[DataRipristino] [datetime],
			[DataEliminazione] [datetime],
			[IdUtenteInserimento] [nvarchar](50),
			[IdUtenteCancellazione] [nvarchar](50),
			[IdUtenteRipristino] [nvarchar](50),
			[IdUtenteEliminazione] [nvarchar](50),
			[Stato] [int],
			[Azione] [nvarchar](50),
			[Note] [json]
		);

		--LOG avvio procedura
		INSERT INTO pfw.ProceduresLog (
			JobId, JobName, JobStepDescription, ProcedureName
			,StartTime, EndTime, Duration, Status, Description)
		VALUES (
			@JobId, @JobName, @JobStepNumber, @ProcedureName
			,@StartTime, NULL, NULL, @ProcedureStatus_Start, @stringFattura)

		-- Controllo parametri
		IF (
			@IdFattura IS NULL
			AND @Anno IS NULL
			AND @Mese IS NULL
			AND @TipologiaFattura IS NULL
			AND @Idente IS NULL
		)
		BEGIN
			-- I parametri passati sono NULL pertanto stoppo esecuzione
			SET @EndTime = GETDATE()

			UPDATE pfw.ProceduresLog
			SET EndTime = @EndTime,
				Status = @ProcedureStatus_Error,
				Duration = DATEDIFF(SECOND,  @StartTime, @EndTime),
				Description = 'I parametri indicati sono NULL, non sono corretti oppure insufficienti. Specificare almeno IdFattura oppure Anno/Mese/Ente/Tipologia Fattura.'
			WHERE JobId = @JobId
			
			PRINT('Parametri non corretti o insufficienti.');

			-- Interrompo procedura con errore
			SELECT 0 as Result;
			RETURN;
		END

		-- reset tabella temporanea
		DELETE FROM @tmpGestioneFatture;

		-- recupera la fattura e storicizza temporaneamente
		INSERT INTO @tmpGestioneFatture(
			 [FkIdFattura]
			,[FkIdEnte]
			,[FkTipologiaFattura]
			,[Anno]
			,[Mese]
			,[Stato]
			,[IdUtenteInserimento]
			,[Azione]
			,[Note]
		)
		SELECT
			ft.IdFattura as FkIdFattura,
			ft.FkIdEnte,
			ft.FkTipologiaFattura,
			ft.AnnoRiferimento as Anno,
			ft.MeseRiferimento as Mese,
			0 as Stato, -- POSTICIPATA
			@IdUtente as IdUtenteInserimento,
			'POSTICIPATA' as Azione,
			@Note as Note
		FROM pfd.FattureTestata ft
		WHERE 
			ft.FkTipologiaFattura in ('PRIMO SALDO','SECONDO SALDO','VAR. SEMESTRALE', 'SEM. SOSPESI')
			AND
			(@IdFattura IS NULL OR ft.IdFattura = @IdFattura)
			AND
			(
				(@Anno IS NULL AND @Mese IS NULL AND @TipologiaFattura IS NULL AND @IdEnte IS NULL) 
				OR (ft.AnnoRiferimento = @Anno
					AND ft.MeseRiferimento = @Mese
					AND ft.FkTipologiaFattura = @TipologiaFattura
					AND ft.FkIdEnte = @IdEnte
					)
			)
			AND (ft.FatturaInviata IS NULL OR ft.FatturaInviata = 0) -- Esclusione fatture già inviate

		DECLARE @countFatture int = 0;
		SELECT @countFatture = COUNT(*) FROM @tmpGestioneFatture

		-- Controlla se esiste la fattura ed è in stato NON INVIATA
		IF (
			@countFatture = 1
		)
		BEGIN            
			-- La fattura esiste ed è in stato NON INVIATA
            
			BEGIN TRANSACTION;
			-- Crea record per Gestione Fatture
			INSERT INTO cfg.GestioneFatture(
				[FkIdFattura]
				,[FkIdEnte]
				,[FkTipologiaFattura]
				,[Anno]
				,[Mese]
				,[IdUtenteInserimento]
				,[Stato]
				,[Azione]
				,[Note]
			)
			SELECT TOP(1)
				[FkIdFattura]
				,[FkIdEnte]
				,[FkTipologiaFattura]
				,[Anno]
				,[Mese]
				,[IdUtenteInserimento]
				,[Stato]
				,[Azione]
				,[Note]
			FROM @tmpGestioneFatture

			COMMIT TRANSACTION

			--LOG completamento stored procedure
			SET @EndTime = GETDATE()

			UPDATE pfw.ProceduresLog
			SET EndTime = @EndTime,
				Status = @ProcedureStatus_End,
				Description = CONCAT('Posticipata Fattura: ',@stringFattura),
				Duration = DATEDIFF(SECOND,  @StartTime, @EndTime)
			WHERE JobId = @JobId

			-- Procedura eseguita correttamente
			SELECT 1 as Result;
			RETURN;

		END
		ELSE
		BEGIN
			-- La fattura non esiste oppure è già stata inviata, termina procedura

			SET @EndTime = GETDATE()

			UPDATE pfw.ProceduresLog
			SET EndTime = @EndTime,
				Status = @ProcedureStatus_Error,
				Duration = DATEDIFF(SECOND,  @StartTime, @EndTime),
				Description = CONCAT('La Fattura: ',@stringFattura,' non esiste oppure è già stata inviata.')
			WHERE JobId = @JobId
			
			PRINT(CONCAT('La Fattura: ',@stringFattura,' non esiste oppure è già stata inviata.'));

			-- Interrompo procedura con errore
			SELECT 0 as Result;
			RETURN;
		END		

		

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

		PRINT(ERROR_MESSAGE())

		-- Procedura terminata con errori
		SELECT 0 as Result;

    END CATCH

END
GO


