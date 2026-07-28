/****** Oggetto: StoredProcedure [be].[spGestioneFatturePosticipa]    Data dello script 28/07/2026 ******/
-- Script autorevole estratto dal DB reale (versione 28/07/2026). CREATE OR ALTER per idempotenza.
-- NOVITA' 28/07:
--   * ora usa MERGE su periodo (UPDATE se il record esiste, INSERT altrimenti) invece dell'INSERT
--     puro -> il ciclo POSTICIPA/RIPRISTINA ripetuto non dovrebbe piu' duplicare la riga.
--   * INSERT usa JSON_ARRAY(@Note) -> note come array.
-- INVARIATO / ancora da correggere:
--   * @IdFattura resta int;
--   * la riga 'SELECT @countFatture = COUNT(*) FROM @tmpGestioneFatture' azzera ancora il conteggio
--     calcolato su FattureTestata -> la guardia 'fattura gia' inviata' resta codice morto e la
--     tipologia non viene validata (ANTICIPO/ACCONTO ancora posticipabili);
--   * il MERGE ON non include [Stato].
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
/*
  Data creazione:        29/06/2026
  Data ultima modifica:  28/07/2026
  Descrizione:           Creazione fattura posticipata
  Target utilizzo:       Pulsante Posticipa della pagina PF "GestioneFatture" / "DocumentiEmessi"
  Versione:              1.0
*/
-- =============================================
CREATE OR ALTER PROCEDURE [be].[spGestioneFatturePosticipa]
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


		-- Veirfica se esiste la fattura ed è in stato = INVIATA
		DECLARE @countFatture int = 0;

		SELECT @countFatture = COUNT(ft.IdFattura)
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
			AND (ft.FatturaInviata = 1)

		SELECT @countFatture = COUNT(*) FROM @tmpGestioneFatture

		-- Controlla se esiste la fattura ed è in stato INVIATA
		IF (
			@countFatture = 1
		)
		BEGIN
			-- la fattura esiste ed è in stato INVIATA
			-- stop procedura con errore

			SET @EndTime = GETDATE()

			declare @message nvarchar(max) = CONCAT('La Fattura: ',@stringFattura,' è già stata inviata.')

			UPDATE pfw.ProceduresLog
			SET EndTime = @EndTime,
				Status = @ProcedureStatus_Error,
				Duration = DATEDIFF(SECOND,  @StartTime, @EndTime),
				Description = @message
			WHERE JobId = @JobId

			PRINT(@message);

			-- Interrompo procedura con errore
			SELECT 0 as Result;
			RETURN;


		END
		ELSE
		BEGIN
			-- La fattura non esiste oppure è in stato = NON INVIATA
			-- procedi con inserimento in tabella GestioneFatture

			BEGIN TRANSACTION;

			SELECT @IdFattura = IdFattura
			FROM pfd.FattureTestata ft
			WHERE
				ft.AnnoRiferimento = @Anno
				AND ft.MeseRiferimento = @Mese
				AND ft.FkIdEnte = @IdEnte
				AND ft.FkTipologiaFattura = @TipologiaFattura


			MERGE INTO cfg.GestioneFatture AS target
			USING ( VALUES(
				@IdFattura
				,@IdEnte
				,@TipologiaFattura
				,@Anno
				,@Mese
				,@IdUtente
				,0
				,'POSTICIPATA'
				,@Note
				))
			AS source (
				FkIdFattura
				,FkIdente
				,FkTipologiaFattura
				,Anno
				,Mese
				,IdUtente
				,Stato
				,Azione
				,Note
				)
			 ON target.Anno = source.Anno
				AND target.Mese = source.Mese
				AND target.FkTipologiaFattura = source.FkTipologiaFattura
				AND target.FkIdEnte = source.FkIdEnte

			WHEN MATCHED THEN
				UPDATE SET
					DataInserimento = GETDATE(),
					Stato = 0,
					Azione = 'POSTICIPATA',
					FkIdFattura = source.FkIdFattura,
					DataCancellazione = NULL,
					IdUtenteInserimento = source.IdUtente,
					IdUtenteCancellazione = NULL,
					[Note] = JSON_MODIFY(
									target.Note,
									'append $',
									JSON_QUERY(source.[Note])
					)

			WHEN NOT MATCHED THEN
				INSERT (
				FkIdFattura
				,FkIdente
				,FkTipologiaFattura
				,Anno
				,Mese
				,IdUtenteInserimento
				,Stato
				,Azione
				,Note
				) VALUES(
				@IdFattura
				,@IdEnte
				,@TipologiaFattura
				,@Anno
				,@Mese
				,@IdUtente
				,0
				,'POSTICIPATA'
				,JSON_ARRAY(@Note)
				);

			COMMIT TRANSACTION

			-- LOG completamento stored procedure
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
