namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

/// <summary>
/// SQL di lettura dell'Invio a SAP: solo SELECT sulle viste <c>be.vw*</c> di DATA (principio §2.1 di
/// be-invio-sap.md). Gli alias coincidono con le proprieta' dei DTO in <c>Dto/InvioSapDto.cs</c>.
/// </summary>
internal static class InvioSapQueryBuilder
{
    public const string OrdineTipologia = @"CASE TipologiaFattura
            WHEN 'ANTICIPO' THEN 1 WHEN 'ACCONTO' THEN 2 WHEN 'PRIMO SALDO' THEN 3 WHEN 'SECONDO SALDO' THEN 4
            WHEN 'VAR. SEMESTRALE' THEN 5 WHEN 'SEM. SOSPESI' THEN 6 WHEN 'VAR. ANNUALE' THEN 7 ELSE 8 END";

    public const string SelectGriglia = @"
        SELECT TipologiaFattura, StatoFattura, NumeroFatture, Anno, Mese, [Importo Totale] AS ImportoTotale
          FROM [be].[vwInvioSAPGriglia]";

    public static string OrderByGriglia => $" ORDER BY Anno, Mese, {OrdineTipologia}, TipologiaFattura, CASE WHEN StatoFattura IS NULL THEN 2 ELSE CAST(StatoFattura AS int) END";

    /// <summary>Il rigo <c>DA_INVIARE</c> della chiave Tipologia/Anno/Mese (D10): il suo <c>NumeroFatture</c>.</summary>
    public const string SelectNumeroFattureRigoDaInviare = @"
        SELECT NumeroFatture
          FROM [be].[vwInvioSAPGriglia]
         WHERE Anno = @Anno AND Mese = @Mese AND TipologiaFattura = @TipologiaFattura AND StatoFattura = 0";

    public const string SelectDettaglio = @"
        SELECT IdFattura, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, RagioneSociale, DataFattura,
               IdentificativoFattura, TotaleFattura, AnnoRiferimento, MeseRiferimento, IdDocumento, Progressivo,
               FatturaInviata, Semestre, DataUltimaGenerazione, NomeFile, NoteInvio
          FROM [be].[vwInvioSAPDettaglio]";

    public const string CountDettaglio = @"
        SELECT COUNT(*)
          FROM [be].[vwInvioSAPDettaglio]";

    public const string OrderByDettaglio = " ORDER BY RagioneSociale, IdFattura";

    // bigint: con Page vicino a int.MaxValue il prodotto (Page - 1) * PageSize in int va in overflow (500).
    public const string OffSet = " OFFSET (CAST(@Page AS bigint) - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string SelectStatoFatture = @"
        SELECT IdFattura, AnnoRiferimento, MeseRiferimento, FkTipologiaFattura, FatturaInviata
          FROM [be].[vwInvioSAPDettaglio]
         WHERE IdFattura IN @IdFatture";

    public const string SelectReinviate = @"
        SELECT IdFattura, TipoDocumento, TipologiaFattura, IdEnte, RagioneSociale, DataFattura, TotaleFattura,
               AnnoRiferimento, MeseRiferimento, Progressivo, ConteggioInvio, Note
          FROM [be].[vwInvioSAPReportFattureReInviate]";

    public const string OrderByReinviate = " ORDER BY AnnoRiferimento, MeseRiferimento, TipologiaFattura, RagioneSociale, IdFattura";

    public const string SelectLock = @"
        SELECT IdAzione, Azione, LockStartUTC, LockTimeoutUTC, BatchId
          FROM [be].[vwLockAttiviAzioniPF]
         WHERE IdAzione = @IdAzione";

    /// <summary>Escape dei caratteri speciali del LIKE (<c>\ % _ [</c>) per la clausola <c>ESCAPE '\'</c>.</summary>
    public static string EscapeLike(string valore) =>
        valore.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_").Replace("[", @"\[");
}
