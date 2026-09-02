using Microsoft.Data.SqlClient;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Ripristino del seed dopo un ELIMINA sul DB seedato.
///
/// Serve perche' la SP reale <c>pfd.EliminaFattura</c> (che ha sostituito lo stub <c>RETURN 1</c>) SPOSTA
/// davvero la fattura in <c>pfd.FattureTestata_Eliminate</c>: chi la elimina deve rimetterla a posto, o le
/// fixture successive lavorano su un seed diverso da quello dichiarato in <c>tests/Data/gestione_fatture.sql</c>.
///
/// Il punto delicato e' la FEDELTA': ricreare la riga con valori segnaposto (il vecchio comportamento:
/// <c>TotaleFattura = 100.00</c>, <c>DataFattura = '2026-01-01'</c>) la fa "sopravvivere" ma con dati diversi
/// dal seed, e rompe silenziosamente ogni test che ne assert i valori — es. la 2001 vale 305.00 nel seed e
/// <c>FattureInvioSapMultiploPeriodoIntegrationTests</c> lo verifica. Qui si ricopia quindi la riga ORIGINALE
/// da <c>*_Eliminate</c> (che la SP ha popolato con tutte le sue colonne), righe comprese; il ramo segnaposto
/// resta solo come fallback per i casi in cui non c'e' stato nessuno spostamento fisico (ELIMINA
/// pre-generazione, che registra lo stato senza toccare <c>pfd.FattureTestata</c>).
/// </summary>
internal static class FattureSeedRestore
{
    /// <summary>
    /// Rimette la fattura come stava prima dell'ELIMINA e ripulisce la riga di staging del periodo.
    /// Best-effort: gli errori SQL non devono mascherare l'esito del test che la chiama.
    /// </summary>
    public static void RipristinaDopoElimina(
        string connectionString, long idFattura, string idEnte, string tipologia, int anno, int mese)
    {
        if (idFattura == 0) return;
        try
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            using var cmd = new SqlCommand(Sql, conn);
            cmd.Parameters.AddWithValue("@id", idFattura);
            cmd.Parameters.AddWithValue("@ente", idEnte);
            cmd.Parameters.AddWithValue("@tipo", tipologia);
            cmd.Parameters.AddWithValue("@anno", anno);
            cmd.Parameters.AddWithValue("@mese", mese);
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { /* best-effort */ }
    }

    // Dopo ELIMINA la riga cfg.GestioneFatture ha FkIdFattura NULL: va rimossa anche per PERIODO,
    // altrimenti resta a occupare la chiave (FkIdEnte, FkTipologiaFattura, Anno, Mese, Stato).
    // IdFattura e' IDENTITY nel DDL reale: per rimettere lo stesso id serve IDENTITY_INSERT.
    private const string Sql = @"
        DELETE FROM cfg.GestioneFatture WHERE FkIdFattura = @id;
        DELETE FROM cfg.GestioneFatture
         WHERE FkIdEnte = @ente AND FkTipologiaFattura = @tipo AND Anno = @anno AND Mese = @mese;

        IF NOT EXISTS (SELECT 1 FROM pfd.FattureTestata WHERE IdFattura = @id)
           AND EXISTS (SELECT 1 FROM pfd.FattureTestata_Eliminate WHERE IdFattura = @id)
        BEGIN
            -- ripristino FEDELE: la riga originale, con i suoi importi/date, e' in *_Eliminate
            SET IDENTITY_INSERT pfd.FattureTestata ON;
            INSERT INTO pfd.FattureTestata
                (IdFattura, FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, FkIdDatiFatturazione,
                 DataFattura, IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento,
                 AnnoRiferimento, MeseRiferimento, CausaleFattura, Sollecito, CodiceContratto, SplitPayment,
                 Cup, Cig, IdDocumento, DataDocumento, NumItem, CodCommessa, Progressivo, FatturaInviata,
                 Semestre, FaseFatturazione)
            SELECT
                 IdFattura, FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, FkIdDatiFatturazione,
                 DataFattura, IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento,
                 AnnoRiferimento, MeseRiferimento, CausaleFattura, Sollecito, CodiceContratto, SplitPayment,
                 Cup, Cig, IdDocumento, DataDocumento, NumItem, CodCommessa, Progressivo, FatturaInviata,
                 Semestre, FaseFatturazione
            FROM pfd.FattureTestata_Eliminate WHERE IdFattura = @id;
            SET IDENTITY_INSERT pfd.FattureTestata OFF;
        END

        INSERT INTO pfd.FattureRighe
            (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile,
             RigaBollo, PeriodoRiferimento, PeriodoFatturazione)
        SELECT
             e.FkIdFattura, e.NumeroLinea, e.Testo, e.CodiceMateriale, e.Quantita, e.PrezzoUnitario, e.Imponibile,
             e.RigaBollo, e.PeriodoRiferimento, e.PeriodoFatturazione
        FROM pfd.FattureRighe_Eliminate e
        WHERE e.FkIdFattura = @id
          AND NOT EXISTS (SELECT 1 FROM pfd.FattureRighe r
                           WHERE r.FkIdFattura = e.FkIdFattura AND r.NumeroLinea = e.NumeroLinea);

        DELETE FROM pfd.FattureRighe_Eliminate WHERE FkIdFattura = @id;
        DELETE FROM pfd.FattureTestata_Eliminate WHERE IdFattura = @id;

        -- Fallback: nessuno spostamento fisico da annullare (ELIMINA pre-generazione) e riga assente.
        -- Valori segnaposto, quindi vale solo per una fattura che nel seed non esisteva.
        IF NOT EXISTS (SELECT 1 FROM pfd.FattureTestata WHERE IdFattura = @id)
        BEGIN
            SET IDENTITY_INSERT pfd.FattureTestata ON;
            INSERT INTO pfd.FattureTestata
                (IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, FatturaInviata,
                 FkProdotto, FkIdTipoDocumento, DataFattura, IdentificativoFattura, TotaleFattura, Divisa,
                 MetodoPagamento, Progressivo)
            VALUES (@id, @ente, @tipo, @anno, @mese, 0,
                 'prod-pn', 'TD01', '2026-01-01', CONCAT('IT-', @id), 100.00, 'EUR', 'MP5', @id);
            SET IDENTITY_INSERT pfd.FattureTestata OFF;
        END";
}
