/****** Oggetto: StoredProcedure [be].[spGestioneFattureElimina]    Data dello script 31/07/2026 ******/
-- Script autorevole estratto dal DB reale. CREATE OR ALTER per idempotenza (unica differenza voluta
-- rispetto al CREATE PROCEDURE dell'originale: serve a ri-applicare lo script su un container gia' avviato).
-- ALLINEAMENTO 31/07: recepita la versione fornita dal team DB. Rispetto alla 29/07 cambiano SOLO
-- diagnostica e codici di uscita, la LOGICA e' identica:
--   * aggiunta PRINT del valore di ritorno di [pfd].[EliminaFattura];
--   * RETURN espliciti e coerenti: 1 sui due rami di successo, 0 su tutti i rami di errore e nel CATCH
--     (prima: 'RETURN;' -> 0 sul successo e RETURN -1 su due rami di errore).
--   NB: i RETURN sono ininfluenti per il backend, che legge il primo result set ('SELECT 0/1 as Result')
--   via QueryFirstAsync<int>; il parametro @ReturnValue della persistence e' dichiarato ma mai letto.
--   L'header della versione fornita riporta ancora 'Data ultima modifica: 30/06/2026' benche' il corpo
--   contenga i fix del 29/07 (Bug A/B): header non aggiornato lato owner, non una versione precedente.
-- NOVITA' 29/07 rispetto alla versione 28/07 (risolve Bug A e Bug B della Elimina):
--   * Bug A (falso "gia' eliminata"): RIMOSSO il ramo 'IF (SELECT FkIdFattura FROM @tmp) IS NOT NULL'
--     che era sempre vero e faceva tornare Result 0 dopo un EXEC pfd.EliminaFattura riuscito. Ora dopo
--     EliminaFattura con @rc>0 si procede al MERGE e si torna Result 1.
--   * Bug B (ELSE eliminava qualsiasi tipologia): il ramo ELSE ora valida la whitelist di tipologia
--     (ANTICIPO/ACCONTO oppure PRIMO SALDO solo per l'ente INPS); le altre tornano Result 0
--     ("Non e' possibile eliminare fatture di SALDO").
-- INVARIATO: @IdFattura resta int; dopo l'eliminazione il record cfg.GestioneFatture ha FkIdFattura NULL
--            (la chiave logica e' il periodo Ente/Tipologia/Anno/Mese).
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
			RETURN 0;
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

			SELECT @IdFattura = FkIdFattura
			FROM @tmpGestioneFatture


			-- Se l'IdFattura è valorizzato allora procedo con l'eliminazione della fattura
			IF (@IdFattura IS NOT NULL)
			BEGIN
				-- Richiama SP Elimina Fattura
				declare @rc int
				EXEC @rc = [pfd].[EliminaFattura] @IdFattura;

				PRINT(CONCAT('VALORE DI RITORNO SP [pfd].[EliminaFattura]:',@rc))

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
					RETURN 0;

				END
			END


			-- Crea/Aggiorna record in Gestione Fatture

			-- è stata PRE-Eliminata la fattura quindi posso annullarla (non c'è l'ID_Fattura)
			BEGIN TRANSACTION;

			MERGE INTO cfg.GestioneFatture as target
			USING (
				SELECT *
				FROM @tmpGestioneFatture
			) as source
			ON
				target.Anno = source.Anno
				AND target.Mese = source.Mese
				AND target.FkIdEnte = source.FkIdEnte
				AND target.FkTipologiaFattura = source.FkTipologiaFattura

			WHEN NOT MATCHED THEN

				INSERT (
					[FkIdFattura]
					,[FkIdEnte]
					,[FkTipologiaFattura]
					,[Anno]
					,[Mese]
					,[IdUtenteInserimento]
					,[Stato]
					,[Azione]
					,[Note]
				)VALUES (
					NULL
					,@IdEnte
					,@TipologiaFattura
					,@Anno
					,@Mese
					,@IdUtente
					,3
					,'ELIMINATA'
					,JSON_ARRAY(@Note)
				)

			WHEN MATCHED THEN	-- Esiste già il record in GestioneFatture quindi lo aggiorno
				UPDATE SET
					DataInserimento = GETDATE(),
					Stato = 3,
					Azione = 'ELIMINATA',
					FkIdFattura = NULL,
					IdUtenteCancellazione = NULL,
					DataCancellazione = NULL,
					[Note] = JSON_MODIFY(
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
				Description = CONCAT('Eliminata Fattura: ',@stringFattura),
				Duration = DATEDIFF(SECOND,  @StartTime, @EndTime)
			WHERE JobId = @JobId

			-- Procedura eseguita correttamente
			SELECT 1 as Result;
			RETURN 1;

		END
		ELSE
		BEGIN
			-- La fattura non esiste in pfd.FattureTestata

			-- Controlla che l'eliminazione della fattura sia coerente con la tipologia fattura
			IF (
				NOT (
					@TipologiaFattura in ('ANTICIPO','ACCONTO')
					OR
					-- TODO: rimuovere eccezione per fattura INPS - PRIMO SALDO
					(@TipologiaFattura = 'PRIMO SALDO' AND @IdEnte='53b40136-65f2-424b-acfb-7fae17e35c60')
				)
			)
			BEGIN
				-- Interrompo la procedura perchè la tipologia fattura non è corretta
				-- Log interruzione per Errore
				SET @EndTime = GETDATE()

				set @err_description = CONCAT('Non è possibile eliminare fatture di SALDO. TipologiaFattura: ',@TipologiaFattura);

				UPDATE pfw.ProceduresLog
				SET EndTime = @EndTime,
					Status = @ProcedureStatus_Error,
					Duration = DATEDIFF(SECOND,  @StartTime, @EndTime),
					Description = @err_description
				WHERE JobId = @JobId

				PRINT(@err_description);

				-- Interrompo procedura con errore
				SELECT 0 as Result;
				RETURN 0;
			END

			-- Controlla che la fattura non esiste nella tabella pfd.FattureTestata_Eliminate
			IF EXISTS (
				SELECT 1
				FROM pfd.FattureTestata_Eliminate
				WHERE AnnoRiferimento = @Anno
					AND MeseRiferimento = @Mese
					AND FkTipologiaFattura = @TipologiaFattura
					AND FkIdEnte = @IdEnte
			)
			BEGIN
				-- Interrompo la procedura perchè la fattura risulta essere già eliminata
				-- Log interruzione per Errore
				SET @EndTime = GETDATE()

				set @err_description = CONCAT('Fattura: ',@stringFattura,' già eliminata.');

				UPDATE pfw.ProceduresLog
				SET EndTime = @EndTime,
					Status = @ProcedureStatus_Error,
					Duration = DATEDIFF(SECOND,  @StartTime, @EndTime),
					Description = @err_description
				WHERE JobId = @JobId

				PRINT(@err_description);

				-- Interrompo procedura con errore
				SELECT 0 as Result;
				RETURN 0;
			END


			-- procedo con insert in GestioneFatture
			BEGIN TRANSACTION;

				MERGE INTO cfg.GestioneFatture as target
				USING (
					VALUES (
						NULL
						,@IdEnte
						,@TipologiaFattura
						,@Anno
						,@Mese
						,@IdUtente
						,3
						,'ELIMINATA'
						,@Note
					)
				) as source (
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
				ON
					target.Anno = source.Anno
					AND target.Mese = source.Mese
					AND target.FkIdEnte = source.FkIdEnte
					AND target.FkTipologiaFattura = source.FkTipologiaFattura

				WHEN NOT MATCHED THEN

					INSERT (
						[FkIdFattura]
						,[FkIdEnte]
						,[FkTipologiaFattura]
						,[Anno]
						,[Mese]
						,[IdUtenteInserimento]
						,[Stato]
						,[Azione]
						,[Note]
					)VALUES (
						NULL
						,@IdEnte
						,@TipologiaFattura
						,@Anno
						,@Mese
						,@IdUtente
						,3
						,'ELIMINATA'
						,JSON_ARRAY(@Note)
					)

				WHEN MATCHED THEN	-- Esiste già il record in GestioneFatture quindi lo aggiorno
					UPDATE SET
						DataInserimento = GETDATE(),
						Stato = 3,
						Azione = 'ELIMINATA',
						FkIdFattura = NULL,
						IdUtenteCancellazione = NULL,
						DataCancellazione = NULL,
						[Note] = JSON_MODIFY(
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
				Description = CONCAT('Eliminata Fattura: ',@stringFattura),
				Duration = DATEDIFF(SECOND,  @StartTime, @EndTime)
			WHERE JobId = @JobId

			-- Procedura eseguita correttamente
			SELECT 1 as Result;
			RETURN 1;

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
		RETURN 0;

    END CATCH

END
GO
