using Microsoft.Data.SqlClient;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// PF-1003: dati di test per il campo <c>fattura.azione</c> di <c>POST api/fatture</c> (ramo EMESSE,
/// <c>FattureQueryRicercaBuilder._sqlView</c>). Condivisi dalla suite integration, dall'adversarial e
/// dalla HTTP.
///
/// Perché le fatture si creano qui e non nel seed: la finestra di <c>be.vwGestioneFattureFormAnniMesi</c>
/// si calcola da <c>GETDATE()</c>, quindi un periodo fisso nel seed oggi è dentro la finestra e fra qualche
/// mese è fuori. Ogni caso ha un <b>offset in mesi</b> rispetto al mese corrente <b>del DB</b> (lo stesso
/// orologio della vista: niente scarti di fuso fra C# e SQL Server).
///
/// Regola attesa (query del 25/11/2026, commit "aggiunta eccezione inps"), nell'ordine del CASE:
/// <list type="number">
/// <item>posticipata o eliminata in <c>cfg.GestioneFatture</c> (Stato 0 o 3) → <c>NULL</c>;</item>
/// <item>ente INPS e tipologia PRIMO SALDO → <c>ELIMINA</c> sempre, anche fuori finestra e anche se inviata;</item>
/// <item>altrimenti l'azione della vista (ELIMINA per ANTICIPO/ACCONTO, POSTICIPA per le altre), <c>NULL</c> fuori
/// finestra. <b>Lo stato di invio non conta più</b>: anche una fattura già inviata a SAP ha l'azione.</item>
/// </list>
/// Le posticipate (Stato 0) non compaiono proprio. In più il campo <c>fattura.stato</c> riporta
/// <c>gf.Stato</c> così com'è (<c>NULL</c> se la fattura non è in Gestione Fatture).
///
/// Pulizia per marcatori, non per elenco di id: <c>IdentificativoFattura LIKE 'PF1003-%'</c>,
/// <c>IdUtenteInserimento = 'PF1003-TEST'</c>, <c>Causale = 'PF1003-TEST'</c>. Così anche un'esecuzione
/// interrotta viene ripulita al giro dopo. Id 7801-7899: liberi nel seed e sotto l'identity corrente,
/// che quindi non viene spostata.
/// </summary>
public static class FattureAzioneTestData
{
    public const string Marcatore = "PF1003-TEST";

    // Ente 1: contratto TOKEN-E1, tipo contratto 2 (PAC). Ente 3: TOKEN-E3, tipo contratto 1 (PAL).
    public const string Ente1 = "11111111-1111-1111-1111-111111111111";
    public const string Ente3 = "33333333-3333-3333-3333-333333333333";
    // INPS: contratto TOKEN-INPS (dal seed gestione_fatture.sql), tipo contratto 2 (PAC). L'eccezione
    // PRIMO SALDO → ELIMINA è scritta con questo GUID nella query.
    public const string Inps = "53b40136-65f2-424b-acfb-7fae17e35c60";

    public const string Elimina = "ELIMINA";
    public const string Posticipa = "POSTICIPA";

    /// <summary>
    /// Un caso: la fattura, il periodo come offset dal mese corrente (0 = mese corrente, 2 = due mesi fa,
    /// -1 = il mese prossimo), lo stato di invio, l'eventuale stato in cfg.GestioneFatture e l'azione attesa.
    /// </summary>
    public sealed record Caso(long Id, string Ente, string Contratto, string Tipologia, int OffsetMesi,
        bool? Inviata, int? StatoGestione, string? AzioneAttesa, string Descrizione)
    {
        public override string ToString() => $"{Id} {Tipologia} {Descrizione}";
    }

    private static Caso E1(long id, string tip, int off, string? atteso, string descr, bool? inviata = false, int? stato = null)
        => new(id, Ente1, "TOKEN-E1", tip, off, inviata, stato, atteso, descr);

    private static Caso E3(long id, string tip, int off, string? atteso, string descr, bool? inviata = false, int? stato = null)
        => new(id, Ente3, "TOKEN-E3", tip, off, inviata, stato, atteso, descr);

    private static Caso In(long id, string tip, int off, string? atteso, string descr, bool? inviata = false, int? stato = null)
        => new(id, Inps, "TOKEN-INPS", tip, off, inviata, stato, atteso, descr);

    /// <summary>Mesi dal mese corrente a gennaio dell'anno dopo il prossimo, cioè il primo mese fuori dalla
    /// finestra in avanti (la vista arriva a dicembre dell'anno prossimo).</summary>
    public static int OffsetOltreFinestra(DateTime oggi) => -(25 - oggi.Month);

    public static IReadOnlyList<Caso> Casi(DateTime oggi) =>
    [
        // --- La regola per tipologia, mese corrente (dentro tutte le finestre) ---
        E3(7801, "ANTICIPO", 0, Elimina, "mese corrente"),
        E3(7802, "ACCONTO", 0, Elimina, "mese corrente"),
        E1(7803, "PRIMO SALDO", 0, Posticipa, "mese corrente"),
        E1(7804, "SECONDO SALDO", 0, Posticipa, "mese corrente"),
        E1(7805, "VAR. SEMESTRALE", 0, Posticipa, "mese corrente"),
        E1(7806, "SEM. SOSPESI", 0, Posticipa, "mese corrente"),
        // Seconda fattura sullo stesso ente/tipologia/periodo di 7804: nessuna duplicazione dalla join.
        E1(7807, "SECONDO SALDO", 0, Posticipa, "stesso periodo di 7804"),

        // --- Stato di invio: dal 25/11/2026 non toglie piu' l'azione ---
        E1(7811, "SECONDO SALDO", 1, Posticipa, "gia' inviata a SAP", inviata: true),
        E1(7812, "PRIMO SALDO", 1, Posticipa, "in elaborazione (FatturaInviata NULL)", inviata: null),
        E1(7813, "ANTICIPO", 1, Elimina, "gia' inviata a SAP (ente 1: su ente 3 c'e' la riga Stato 1 di 7821)", inviata: true),
        E1(7814, "SECONDO SALDO", 4, null, "gia' inviata e fuori finestra: decide la finestra", inviata: true),

        // --- Stato in cfg.GestioneFatture ---
        E3(7821, "ANTICIPO", 1, Elimina, "ripristinata (Stato 1): si puo' agire di nuovo", stato: 1),
        E3(7822, "ACCONTO", 1, Elimina, "cancellata (Stato 2): si puo' agire di nuovo", stato: 2),
        E1(7823, "SEM. SOSPESI", 1, null, "eliminata non ancora elaborata (Stato 3)", stato: 3),
        E1(7824, "VAR. SEMESTRALE", 1, null, "posticipata (Stato 0): fuori dalla griglia", stato: 0),

        // --- Bordi delle finestre (offset: ANTICIPO e PRIMO SALDO 2, ACCONTO e VAR/SEM 1, SECONDO SALDO 3) ---
        E3(7831, "ANTICIPO", 2, Elimina, "primo mese della finestra"),
        E3(7832, "ANTICIPO", 3, null, "un mese prima della finestra"),
        E3(7833, "ACCONTO", 2, null, "un mese prima della finestra"),
        E1(7834, "SECONDO SALDO", 3, Posticipa, "primo mese della finestra"),
        E1(7835, "SECONDO SALDO", 4, null, "un mese prima della finestra"),
        E1(7836, "PRIMO SALDO", 2, Posticipa, "primo mese della finestra"),
        E1(7837, "PRIMO SALDO", 3, null, "un mese prima della finestra"),
        E1(7838, "VAR. SEMESTRALE", 2, null, "un mese prima della finestra"),
        E1(7839, "SEM. SOSPESI", 2, null, "un mese prima della finestra"),

        // --- Periodi futuri ---
        E1(7841, "SECONDO SALDO", -1, Posticipa, "mese prossimo"),
        E3(7842, "ANTICIPO", OffsetOltreFinestra(oggi) + 1, Elimina, "dicembre dell'anno prossimo (ultimo mese)"),
        E3(7843, "ANTICIPO", OffsetOltreFinestra(oggi), null, "gennaio fra due anni (oltre la finestra)"),

        // --- Eccezione INPS: PRIMO SALDO -> sempre ELIMINA. Un periodo per caso: cfg.GestioneFatture si
        //     aggancia per ente+tipologia+anno+mese, due casi sullo stesso periodo si influenzerebbero. ---
        In(7851, "PRIMO SALDO", 0, Elimina, "INPS mese corrente: ELIMINA invece di POSTICIPA (cfr. 7803)"),
        In(7852, "PRIMO SALDO", 3, Elimina, "INPS fuori finestra: l'eccezione non guarda la vista (cfr. 7837)"),
        In(7853, "PRIMO SALDO", 1, Elimina, "INPS gia' inviata a SAP", inviata: true),
        In(7854, "PRIMO SALDO", 4, null, "INPS eliminata (Stato 3): lo stato vince sull'eccezione", stato: 3),
        In(7855, "PRIMO SALDO", 5, null, "INPS posticipata (Stato 0): fuori dalla griglia", stato: 0),
        In(7856, "PRIMO SALDO", 6, Elimina, "INPS ripristinata (Stato 1)", stato: 1),
        In(7857, "PRIMO SALDO", 7, Elimina, "INPS cancellata (Stato 2)", stato: 2),
        In(7858, "SECONDO SALDO", 0, Posticipa, "INPS altra tipologia di saldo: niente eccezione"),
        In(7859, "ANTICIPO", 0, Elimina, "INPS anticipo: ELIMINA dalla vista"),
        In(7860, "SECONDO SALDO", 4, null, "INPS altra tipologia fuori finestra: niente eccezione"),
    ];

    /// <summary>Periodo (anno, mese) di un caso, calcolato dal mese corrente.</summary>
    public static (int Anno, int Mese) Periodo(DateTime oggi, Caso c)
    {
        var d = new DateTime(oggi.Year, oggi.Month, 1).AddMonths(-c.OffsetMesi);
        return (d.Year, d.Month);
    }

    /// <summary>Data corrente del DB: stesso orologio della vista.</summary>
    public static DateTime OggiDb(string connectionString)
    {
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = new SqlCommand("SELECT GETDATE()", conn);
        return (DateTime)cmd.ExecuteScalar()!;
    }

    // Tipologie che nel seed non hanno configurazione per il tipo contratto 2: senza, l'INNER JOIN con
    // pfw.FatturaTestataConfig scarterebbe le fatture prima ancora di calcolare l'azione.
    // ANTICIPO serve per 7859 (INPS, tipo contratto 2); l'IF NOT EXISTS lascia stare una config gia' presente.
    private static readonly string[] ConfigDaAggiungere = ["PRIMO SALDO", "VAR. SEMESTRALE", "SEM. SOSPESI", "ANTICIPO"];

    public static void Inserisci(string connectionString, DateTime oggi)
    {
        Pulisci(connectionString);
        using var conn = new SqlConnection(connectionString);
        conn.Open();

        foreach (var tip in ConfigDaAggiungere)
            Esegui(conn, """
                IF NOT EXISTS (SELECT 1 FROM pfw.FatturaTestataConfig WHERE FKProdotto = 'prod-pn' AND FKIdTipoContratto = 2 AND FkTipologiaFattura = @tip)
                INSERT INTO pfw.FatturaTestataConfig
                 (FKProdotto, FKIdTipoContratto, FkTipologiaFattura, FKTipoDocumentoFattura, FKTipoDocumentoNotaCredito, FKIdMetodoPagamento, Divisa, Causale)
                VALUES ('prod-pn', 2, @tip, 'TD01', 'TD04', 5, 'EUR', @marcatore)
                """, ("@tip", tip), ("@marcatore", Marcatore));

        foreach (var c in Casi(oggi))
        {
            var (anno, mese) = Periodo(oggi, c);
            Esegui(conn, """
                SET IDENTITY_INSERT pfd.FattureTestata ON;
                INSERT INTO pfd.FattureTestata
                 (IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, FatturaInviata,
                  FkProdotto, FkIdTipoDocumento, DataFattura, IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento, Progressivo, CodiceContratto)
                VALUES (@id, @ente, @tip, @anno, @mese, @inviata,
                  'prod-pn', 'TD01', DATEFROMPARTS(@anno, @mese, 1), CONCAT('PF1003-', @id), 100.00, 'EUR', 'MP5', @id, @contratto);
                SET IDENTITY_INSERT pfd.FattureTestata OFF;
                INSERT INTO pfd.FattureRighe (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
                VALUES (@id, 1, 'riga PF-1003', 'MAT-A', 1, 100.00, 100.00, 0, CONCAT(FORMAT(@mese, '00'), '/', @anno));
                """,
                ("@id", c.Id), ("@ente", c.Ente), ("@tip", c.Tipologia), ("@anno", anno), ("@mese", mese),
                ("@inviata", (object?)c.Inviata ?? DBNull.Value), ("@contratto", c.Contratto));

            if (c.StatoGestione is int stato)
                Esegui(conn, """
                    INSERT INTO cfg.GestioneFatture (FkIdEnte, FkTipologiaFattura, Anno, Mese, FkIdFattura, IdUtenteInserimento, Stato, Azione)
                    VALUES (@ente, @tip, @anno, @mese, @id, @marcatore, @stato, @azione)
                    """,
                    ("@ente", c.Ente), ("@tip", c.Tipologia), ("@anno", anno), ("@mese", mese), ("@id", c.Id),
                    ("@marcatore", Marcatore), ("@stato", stato),
                    ("@azione", stato switch { 0 => "POSTICIPATA", 1 => "RIPRISTINATA", 2 => "CANCELLATA", _ => "ELIMINATA" }));
        }
    }

    public static void Pulisci(string connectionString)
    {
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        Esegui(conn, """
            DELETE FROM cfg.GestioneFatture WHERE IdUtenteInserimento = @marcatore;
            DELETE r FROM pfd.FattureRighe r JOIN pfd.FattureTestata t ON t.IdFattura = r.FkIdFattura WHERE t.IdentificativoFattura LIKE 'PF1003-%';
            DELETE FROM pfd.FattureTestata WHERE IdentificativoFattura LIKE 'PF1003-%';
            DELETE FROM pfw.FatturaTestataConfig WHERE Causale = @marcatore;
            """, ("@marcatore", Marcatore));
    }

    private static void Esegui(SqlConnection conn, string sql, params (string Nome, object? Valore)[] parametri)
    {
        using var cmd = new SqlCommand(sql, conn);
        foreach (var (nome, valore) in parametri)
            cmd.Parameters.AddWithValue(nome, valore ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }
}
