/****** Oggetto: StoredProcedure [be].[spGestioneFattureCancella]    Data dello script 28/07/2026 ******/
-- Script autorevole estratto dal DB reale (versione 28/07/2026). CREATE OR ALTER per idempotenza.
-- NOTA (28/07): questa versione NON risolve ancora i 4 difetti segnalati in
-- coverage/segnalazione-sp-gestione-fatture.md:
--   1) @IdFattura resta 'int' (non bigint) -> troncamento per valori > int.MaxValue;
--   2) filtro 'AND gf.FkIdFattura = @IdFattura' resta -> mismatch con record per-periodo (FkIdFattura NULL);
--   3) MERGE ON senza [Stato] -> con due record (Stato 0 e 3) sullo stesso periodo viola la PK;
--   4) @err_message resta solo in ProceduresLog/PRINT, non nel SELECT -> l'endpoint non riceve il motivo.
-- I test di caratterizzazione restano quindi verdi (comportamento invariato).
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
/*
  Data creazione:        30/06/2026
  Data ultima modifica:  28/07/2026
  Descrizione:           Cancella fattura da Gestione Fatture, se l'utente admin ha sbagliato a Eliminare/Posticipare la fattura
  Target utilizzo:       Pulsante Cancella della pagina PF "GestioneFatture"
  Versione:              1.0
*/
-- =============================================
CREATE OR ALTER PROCEDURE [be].[spGestioneFattureCancella]
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
    DECLARE @JobName VARCHAR(100) = 'CANCELLA FATTURA';
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

		-- Controllo parametri SP
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


		-- Controllo fattura eliminata se già presente in FattureTestata_Eliminate
		IF EXISTS (
			SELECT 1
			FROM pfd.FattureTestata_Eliminate fte
			WHERE
				fte.AnnoRiferimento = @Anno
				AND fte.MeseRiferimento = @Mese
				AND fte.FkTipologiaFattura = @TipologiaFattura
				AND fte.FkIdEnte = @IdEnte
		)
		BEGIN
			-- La fattura è già stata eliminata
			SET @EndTime = GETDATE()

			declare @err_message nvarchar(500) = concat('Fattura:', @stringFattura, ' è già stata eliminata, non si può cancellare.')

			UPDATE pfw.ProceduresLog
			SET EndTime = @EndTime,
				Status = @ProcedureStatus_Error,
				Duration = DATEDIFF(SECOND,  @StartTime, @EndTime),
				Description = @err_message
			WHERE JobId = @JobId

			PRINT(@err_message);

			-- Interrompo procedura con errore
			SELECT 0 as Result;
			RETURN;
		END

		-- recupera la fattura e storicizzala temporaneamente
		INSERT INTO @tmpGestioneFatture(
			 [FkIdEnte]
			,[FkTipologiaFattura]
			,[Anno]
			,[Mese]
			,[Stato]
			,[IdUtenteCancellazione]
			,[Azione]
			,[DataCancellazione]
			,[Note]
		)
		SELECT
			gf.FkIdEnte,
			gf.FkTipologiaFattura,
			gf.Anno,
			gf.Mese,
			2 as Stato, -- CANCELLATA
			@IdUtente as IdUtenteRipristino,
			'CANCELLATA' as Azione,
			GETDATE() as DataCancellazione,
			@Note as Note
		FROM cfg.GestioneFatture gf
		WHERE
		(
			( -- controllo le Posticipate
				gf.Stato = 0
				AND gf.FkTipologiaFattura in ('PRIMO SALDO','SECONDO SALDO','VAR. SEMESTRALE', 'SEM. SOSPESI')
			)
			OR
			( -- controllo le Eliminate
				gf.Stato = 3
				AND
				(gf.FkTipologiaFattura in ('ANTICIPO','ACCONTO')
				-- TODO: rimuovere eccezione per fattura INPS - PRIMO SALDO
				or (gf.FkIdEnte='53b40136-65f2-424b-acfb-7fae17e35c60' and gf.FkTipologiaFattura='PRIMO SALDO')
				)
			)
		) -- Prendi solo le fatture posticipate=0 oppure eliminate=3
		AND
		(@IdFattura IS NULL OR gf.FkIdFattura = @IdFattura)
		AND
		(
			(@Anno IS NULL AND @Mese IS NULL AND @TipologiaFattura IS NULL AND @IdEnte IS NULL)
			OR (gf.Anno = @Anno
				AND gf.Mese = @Mese
				AND gf.FkTipologiaFattura = @TipologiaFattura
				AND gf.FkIdEnte = @IdEnte
				)
		)


		DECLARE @countFatture int = 0;
		SELECT @countFatture = COUNT(*) FROM @tmpGestioneFatture

		-- Controlla se esiste la fattura ed è in stato POSTICIPATA/Eliminata
		IF (
			@countFatture = 1
		)
		BEGIN
			-- La fattura esiste ed è in stato POSTICIPATA/Eliminata

			BEGIN TRANSACTION;
			-- Aggiorna record Gestione Fatture

			MERGE cfg.GestioneFatture AS target
			USING (
				SELECT
					[FkIdEnte]
					,[FkTipologiaFattura]
					,[Anno]
					,[Mese]
					,[Stato]
					,[IdUtenteCancellazione]
					,[Azione]
					,[DataCancellazione]
					,[Note]
				FROM @tmpGestioneFatture
			) source
				ON	target.anno = source.anno
					AND target.mese = source.mese
					AND target.[FkTipologiaFattura] = source.[FkTipologiaFattura]
					AND target.[FkIdEnte] = source.[FkIdEnte]
			WHEN MATCHED THEN
				UPDATE
					SET Stato = source.Stato
						,[DataCancellazione] = source.[DataCancellazione]
						,[IdUtenteCancellazione] = source.[IdUtenteCancellazione]
						,[Azione] = source.[Azione]
						,[Note] = JSON_MODIFY(
									target.Note,
									'append $',
									JSON_QUERY(source.[Note])
						);

			COMMIT TRANSACTION

			--LOG completamento stored procedure
			SET @EndTime = GETDATE()

			UPDATE pfw.ProceduresLog
			SET EndTime = @EndTime,
				Status = @ProcedureStatus_End,
				Description = CONCAT('Cancellata Fattura: ',@stringFattura),
				Duration = DATEDIFF(SECOND,  @StartTime, @EndTime)
			WHERE JobId = @JobId

			-- Procedura eseguita correttamente
			SELECT 1 as Result;
			RETURN;

		END
		ELSE
		BEGIN
			-- La fattura non esiste in GestioneFatture, termina procedura

			declare @err_description nvarchar(100) = CONCAT('La Fattura: ',@stringFattura,' non esiste in gestione fatture oppure non ha uno stato corretto (Stato Eliminata = 3 oppure Stato Posticipata = 0).')

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
