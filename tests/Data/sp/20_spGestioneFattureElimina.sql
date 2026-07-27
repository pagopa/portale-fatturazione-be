/****** Oggetto: StoredProcedure [be].[spGestioneFattureElimina]    Data dello script 24/07/2026 14:35:59 ******/
-- Script autorevole estratto dal DB reale. Nel DB e' un ALTER: qui CREATE OR ALTER, cosi' funziona
-- sia su container appena creato sia riapplicato a caldo su uno gia' avviato.
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
/*
  Data creazione:        30/06/2026
  Data ultima modifica:  30/06/2026
  Descrizione:           Elimina fattura emessa
  Target utilizzo:       Pulsante Elimina della pagina PF "GestioneFatture" / "DocumentiEmessi"
  Versione:              1.0
*/
-- =============================================
CREATE OR ALTER PROCEDURE [be].[spGestioneFattureElimina]
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
    DECLARE @JobName VARCHAR(100) = 'ELIMINA FATTURA';
    DECLARE @ProcedureName VARCHAR(100) = OBJECT_NAME(@@PROCID);
    DECLARE @JobStepNumber VARCHAR(500) = '1';
    DECLARE @StartTime DATETIME = GETDATE();
    DECLARE @EndTime DATETIME;
    DECLARE @ProcedureStatus_Start VARCHAR(10) = 'START';
    DECLARE @ProcedureStatus_End VARCHAR(10) = 'COMPLETED';
    DECLARE @ProcedureStatus_Error VARCHAR(10) = 'ERROR';

	declare @err_description nvarchar(100) = ''

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
			3 as Stato, -- ELIMINATA
			@IdUtente as IdUtenteInserimento,
			'ELIMINATA' as Azione,
			@Note as Note
		FROM pfd.FattureTestata ft
		WHERE
			(ft.FkTipologiaFattura in ('ANTICIPO','ACCONTO')
			-- TODO: rimuovere eccezione per fattura INPS - PRIMO SALDO
			or (ft.FkIdEnte='53b40136-65f2-424b-acfb-7fae17e35c60' and ft.FkTipologiaFattura='PRIMO SALDO')
			)
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

			-- Se l'IdFattura è valorizzato allora procedo con l'eliminazione della fattura
			IF (@IdFattura IS NOT NULL)
				BEGIN
					-- Richiama SP Elimina Fattura
					declare @rc int
					EXEC @rc = [pfd].[EliminaFattura] @IdFattura;

					IF(@rc <= 0)
					BEGIN

						-- Log interruzione per Errore
						SET @EndTime = GETDATE()

						set @err_description = 'SP [pfd].[EliminaFattura] riporta errori di esecuzione.';

						UPDATE pfw.ProceduresLog
						SET EndTime = @EndTime,
							Status = @ProcedureStatus_Error,
							Duration = DATEDIFF(SECOND,  @StartTime, @EndTime),
							Description = @err_description
						WHERE JobId = @JobId

						PRINT(@err_description);

						-- Interrompo procedura con errore
						SELECT 0 as Result;
						RETURN -1;

					END
				END
			ELSE
				BEGIN

					-- La fattura non esiste in pfd.FattureTestata quindi non è necessario eseguire ulteriori operazioni

					--LOG completamento stored procedure
					SET @EndTime = GETDATE()

					UPDATE pfw.ProceduresLog
					SET EndTime = @EndTime,
						Status = @ProcedureStatus_End,
						Description = CONCAT('La fattura non esiste in pfd.FattureTestata: ',@stringFattura),
						Duration = DATEDIFF(SECOND,  @StartTime, @EndTime)
					WHERE JobId = @JobId

					-- Procedura eseguita correttamente
					SELECT 1 as Result;
					RETURN;
				END


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
				Description = CONCAT('Eliminata Fattura: ',@stringFattura),
				Duration = DATEDIFF(SECOND,  @StartTime, @EndTime)
			WHERE JobId = @JobId

			-- Procedura eseguita correttamente
			SELECT 1 as Result;
			RETURN;

		END
		ELSE
		BEGIN
			-- La fattura non esiste oppure è già stata inviata, termina procedura

			set @err_description = concat('La Fattura: ',@stringFattura,' non esiste oppure è già stata inviata.')

			SET @EndTime = GETDATE()

			UPDATE pfw.ProceduresLog
			SET EndTime = @EndTime,
				Status = @ProcedureStatus_Error,
				Duration = DATEDIFF(SECOND,  @StartTime, @EndTime),
				Description = @err_description
			WHERE JobId = @JobId

			PRINT(@err_description);

			-- Interrompo procedura con errore
			SELECT 0 as Result;
			RETURN -1;
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
