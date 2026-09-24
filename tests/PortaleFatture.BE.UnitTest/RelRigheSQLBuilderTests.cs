using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence.Builder;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// La SELECT del "Report di dettaglio notifiche". Dal 21/09/2026 (PF-882) le colonne `anno` e `mese`
/// del file non vengono piu' dal periodo della REL (`r.year`/`r.month`) ma da quello della
/// **notifica** (`AnnoNotifica`/`MeseNotifica`), con le righe originali lasciate commentate accanto.
///
/// Il filtro invece NON e' cambiato: sta in `RelRigheQueryGetByIdPersistence` e usa ancora
/// `r.year`/`r.month` (o `r.FlagConguaglio` per la VAR. SEMESTRALE). Il file seleziona quindi per
/// periodo di fatturazione e mostra il periodo di competenza: e' il senso della modifica.
///
/// Questo e' un test **deliberatamente povero** — asserisce su una stringa SQL, non su un
/// comportamento — e da solo non proverebbe granche'. Esiste per un motivo preciso: le due righe
/// vecchie sono **commentate e non rimosse**, quindi scambiarle di nuovo e' un'operazione di due
/// caratteri che nessun'altra rete intercetterebbe in locale (il seed non valorizza quelle colonne,
/// v. `docs/test-integrazione-db-seedato.md`). La verifica sul comportamento vero, con valori
/// diversi fra i due periodi, e' in `RelRigheAnnoNotificaIntegrationTests`.
/// </summary>
public class RelRigheSQLBuilderTests
{
    [Test]
    public void SelectAll_ShouldProiettareAnnoEMeseDellaNotifica()
    {
        var sql = RelRigheSQLBuilder.SelectAll();

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("AnnoNotifica AS anno"));
            Assert.That(sql, Does.Contain("MeseNotifica AS mese"));
        });
    }

    /// <summary>
    /// Le righe `r.[year] AS anno` / `r.[month] AS mese` possono restare nel file, ma solo commentate:
    /// attive produrrebbero un alias duplicato e la colonna vincente dipenderebbe dall'ordine.
    /// </summary>
    [Test]
    public void SelectAll_LeProiezioniDelPeriodoRel_ShouldRestareCommentate()
    {
        var righeAttive = RelRigheSQLBuilder.SelectAll()
            .Split('\n')
            .Select(r => r.Trim())
            .Where(r => !r.StartsWith("--"))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(righeAttive, Has.None.Contains("r.[year] AS anno"),
                "il periodo della REL non deve tornare a riempire la colonna anno del CSV");
            Assert.That(righeAttive, Has.None.Contains("r.[month] AS mese"));
        });
    }
}
