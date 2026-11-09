-- Vista pfd.vw*. DDL reale fornita il 06/11/2026, riprodotta as-is
-- (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Dati per il PDF di una fattura emessa: testata, Rel definitiva, sospese comprese (via pfd.MesiFatture)
-- con la loro Rel temporanea, anticipi/acconti/storni aggregati dalle righe. Letta da
-- FattureQueryRicercaBuilder (download del documento della fattura emessa,
-- FatturaEmessaDocumentoDownloadHttpTests).
CREATE OR ALTER VIEW [pfd].[vwFatturaEmessaDownloadPDF]
 AS
 SELECT
            tft.IdFattura as IdFattura,
            e.description as RagioneSociale,
            tft.FkIdTipoDocumento as TipoDocumento,
            tft.FkIdEnte as IdEnte,
            tft.DataFattura as DataFattura,
            tft.Progressivo as Progressivo,
            tft.IdDocumento,
            CAST(
            CASE
            WHEN tft.FkIdTipoDocumento  = 'TD04'
                THEN -tft.TotaleFattura
            ELSE tft.TotaleFattura
                 END AS decimal(18,2)) as TotaleFatturaImponibile,
            tft.CodiceContratto as IdContratto,
            tft.AnnoRiferimento as Anno,
            tft.MeseRiferimento as Mese,
            tft.FkTipologiaFattura AS TipologiaFattura,
            ISNULL(trt.[TotaleAnalogico],0) as RelTotaleAnalogico,
            ISNULL(trt.[TotaleDigitale],0) as RelTotaleDigitale,
            ISNULL(trt.[TotaleNotificheAnalogiche],0) as RelTotaleNotificheAnalogiche,
            ISNULL(trt.[TotaleNotificheDigitali],0) as RelTotaleNotificheDigitali,
            ISNULL(trt.[TotaleNotificheAnalogiche],0) + ISNULL(trt.[TotaleNotificheDigitali],0) as RelTotaleNotifiche,
            ISNULL(trt.[Totale],0) as RelTotale,
            ISNULL(trt.[TotaleAnalogicoIva],0)  as RelTotaleIvatoAnalogico,
            ISNULL(trt.[TotaleDigitaleIva],0)  as RelTotaleIvatoDigitale,
            ISNULL(trt.[TotaleIva],0)  as RelTotaleIvato,
            trt.[Caricata] as Caricata,
            trt.[RelFatturata],
            c.FkIdTipoContratto,
            tp.Descrizione as TipologiaContratto,
            ISNULL(aggAnticipo.ImportoAnalogico, 0) AS AnticipoAnalogico,
            ISNULL(aggAnticipo.ImportoDigitale, 0) AS AnticipoDigitale,
            ISNULL(aggAcconto.ImportoAnalogico, 0) AS AccontoAnalogico,
            ISNULL(aggAcconto.ImportoDigitale, 0) AS AccontoDigitale,
            aggStorno.StornoAnalogico,
            aggStorno.StornoDigitale
            -- Dettaglio fatture sospese compresee nella fattura emessa
            ,mf.FkIdFatturaTmp As IdFatturaSospesa
            ,tftt.DataFattura AS DataFatturaSospesa
            ,tftt.Progressivo AS ProgressivoSospesa
            ,tftt.FkIdTipoDocumento AS TipoDocumentoSospesa,
            CAST(
            CASE
            WHEN tftt.FkIdTipoDocumento  = 'TD04'
                THEN -tftt.TotaleFattura
            ELSE tftt.TotaleFattura
                 END AS decimal(18,2)) as TotaleFatturaSospesaImponibile,
            tftt.CodiceContratto as IdContrattoSospesa,
            tftt.AnnoRiferimento as AnnoSospesa,
            tftt.MeseRiferimento as MeseSospesa,
            tftt.FkTipologiaFattura AS TipologiaFatturaSospesa,
            tftt.TotaleFattura as TotaleFatturaSospesa,
            tftt.MetodoPagamento as MetodoPagamentoSospesa,
            tftt.CausaleFattura as CausaleFatturaSospesa,
            tftt.Sollecito as SollecitoSospesa,
            tftt.SplitPayment as SplitPaymentSospesa,
            tftt.DataDocumento as DataDocumentoSospeso,
            tftt.CodCommessa AS CodiceCommessaSospesa,
            tftt.NumItem AS NumItemSospesa,
            tftt.FatturaInviata AS FatturaInviataSospesa,
            tftt.Semestre AS SemestreSospesa,
            tftt.FlagFatturata AS FlagFatturataSospesa,
            ISNULL(tmprl.[TotaleAnalogico],0) as RelTotaleAnalogicoSospeso,
            ISNULL(tmprl.[TotaleDigitale],0) as RelTotaleDigitaleSospeso,
            ISNULL(tmprl.[TotaleNotificheAnalogiche],0) as RelTotaleNotificheAnalogicheSospeso,
            ISNULL(tmprl.[TotaleNotificheDigitali],0) as RelTotaleNotificheDigitaliSospeso,
            ISNULL(tmprl.[TotaleNotificheAnalogiche],0) + ISNULL(tmprl.[TotaleNotificheDigitali],0) as RelTotaleNotificheSospeso,
            ISNULL(tmprl.[Totale],0) as RelTotaleSospeso,
            ISNULL(tmprl.[TotaleAnalogicoIva],0)  as RelTotaleIvatoAnalogicoSospeso,
            ISNULL(tmprl.[TotaleDigitaleIva],0)  as RelTotaleIvatoDigitaleSospeso,
            ISNULL(tmprl.[TotaleIva],0)  as RelTotaleIvatoSospeso,
            tmprl.[Caricata] as CaricataSospeso,
            tmprl.[RelFatturata] as RelFatturataSospeso,
            iva.Iva
            FROM pfd.FattureTestata tft
                LEFT OUTER JOIN [pfd].[MesiFatture] mf
                ON tft.IdFattura = mf.FkIdFattura
                LEFT OUTER JOIN pfd.Enti e
                ON e.InternalIstitutionId = tft.FkIdEnte
                LEFT OUTER JOIN [pfd].[RelTestata] trt
                ON trt.year = tft.AnnoRiferimento
                    AND trt.month = tft.MeseRiferimento
                    --AND trt.TipologiaFattura = tft.FkTipologiaFattura
                    AND trt.internal_organization_id = tft.FkIdEnte
                    AND trt.contract_id = tft.CodiceContratto
                LEFT JOIN pfd.Contratti c
                ON c.internalistitutionid = e.InternalIstitutionId
                LEFT JOIN pfw.TipoContratto tp
                ON c.FkIdTipoContratto = tp.IdTipoContratto
                LEFT JOIN pfd.tmpFattureTestata tftt
                ON tftt.IdFattura = mf.FkIdFatturaTmp
                LEFT JOIN pfd.tmpRelTestata tmprl
                ON tftt.FkIdEnte = tmprl.internal_organization_id
                    AND tftt.CodiceContratto = tmprl.contract_id
                    --AND tftt.FkTipologiaFattura = tmprl.TipologiaFattura
                    AND tftt.AnnoRiferimento = tmprl.year
                    AND tftt.MeseRiferimento = tmprl.month
                LEFT JOIN pfd.Iva iva
                ON iva.Anno = tft.AnnoRiferimento AND iva.Mese = tft.MeseRiferimento

            -- OUTER APPLY per fatture ANTICIPO
            OUTER APPLY (
                SELECT
                    SUM(CASE WHEN
                                   rt.CodiceMateriale = 'STORNO 50% ANT.NA sospeso'     -- storni Sospesi PAC
                                OR rt.CodiceMateriale = 'STORNO ANTICIPO NA sospeso'    -- storni Sospesi PAL
                                OR rt.CodiceMateriale = 'STORNO 50% ANT.NA'             -- storni Emesse PAC
                                OR rt.CodiceMateriale = 'STORNO ANTICIPO NA'            -- storni Emesse PAL
                             THEN rt.Imponibile ELSE 0 END) AS ImportoAnalogico,
                    SUM(CASE WHEN
                                   rt.CodiceMateriale = 'STORNO 50% ANT.ND sospeso'     -- storni Sospesi PAC
                                OR rt.CodiceMateriale = 'STORNO ANTICIPO ND sospeso'    -- storni Sospesi PAL
                                OR rt.CodiceMateriale = 'STORNO 50% ANT.ND'             -- storni Emesse PAC
                                OR rt.CodiceMateriale = 'STORNO ANTICIPO ND'            -- storni Emesse PAL
                             THEN rt.Imponibile ELSE 0 END) AS ImportoDigitale
                FROM pfd.tmpFattureRighe rt
                WHERE rt.FkIdFattura = tftt.IdFattura
               --   AND tft.FkTipologiaFattura = 'ANTICIPO'
            ) aggAnticipo
            -- OUTER APPLY per fatture ACCONTO
            OUTER APPLY (
                SELECT
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'ACCONTO NOT.ANL%' OR rt.CodiceMateriale = 'STORNO ACCONTO NA'
                             THEN rt.Imponibile ELSE 0 END) AS ImportoAnalogico,
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'ACCONTO NOT.DIG%' OR rt.CodiceMateriale = 'STORNO ACCONTO ND'
                             THEN rt.Imponibile ELSE 0 END) AS ImportoDigitale
                FROM pfd.FattureRighe rt
                WHERE rt.FkIdFattura = tft.IdFattura
                 -- AND tft.FkTipologiaFattura = 'ACCONTO'
            ) aggAcconto
            OUTER APPLY (
                SELECT
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'STORN%' AND rt.CodiceMateriale LIKE '%NA%'
                             THEN rt.Imponibile ELSE 0 END) AS StornoAnalogico,
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'STORN%' AND rt.CodiceMateriale LIKE '%ND%'
                             THEN rt.Imponibile ELSE 0 END) AS StornoDigitale
                FROM pfd.tmpFattureRighe rt
                WHERE rt.FkIdFattura = tftt.IdFattura
                  --AND tftt.FkTipologiaFattura = 'PRIMO SALDO'
                  -- FORSE NON NECESSARIO -da controllare quando abbiamo storni di Secondo Saldo
            ) aggStorno
            WHERE NOT EXISTS (
                SELECT 1
                FROM pfd.tmpFattureTestata t_all
                INNER JOIN [pfd].[MesiFatture] mf_all
                    ON t_all.IdFattura = mf_all.FkIdFatturaTmp
                WHERE mf_all.FkIdFattura = tft.IdFattura
                  AND (
                        t_all.AnnoRiferimento * 100 + t_all.MeseRiferimento) >
                        (tft.AnnoRiferimento * 100 + tft.MeseRiferimento)
                  AND tftt.FkTipologiaFattura = tft.FkTipologiaFattura

                )
GO
