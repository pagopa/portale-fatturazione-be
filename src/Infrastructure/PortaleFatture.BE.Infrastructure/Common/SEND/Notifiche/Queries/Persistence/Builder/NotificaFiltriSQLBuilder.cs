using System.Text.RegularExpressions;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;

/// <summary>
/// I filtri della ricerca notifiche, estratti dalle due Persistence che li componevano inline
/// (`NotificaQueryGetByListEntiPersistence` e la sua gemella `...v2`).
///
/// Il WHERE era **identico carattere per carattere** nelle due classi, duplicato per copia: qui c'e'
/// una sola volta. La classe e' `public` — a differenza di `NotificaSQLBuilder`, che e' `internal` e
/// quindi non verificabile dai progetti di test — proprio perche' l'invariante che protegge merita di
/// essere asseribile senza database.
///
/// **L'invariante**: ogni `@segnaposto` che finisce nel WHERE deve avere un parametro corrispondente
/// in `Parametri`. Sembra ovvia, ed e' esattamente quella che la riscrittura v2 ha perso — passando al
/// `SqlCommand` solo quattro parametri su quattordici, con l'insieme completo rimasto nel codice ma
/// non piu' letto da nessuno. Un difetto invisibile in review e visibile in un test di due righe:
/// `NotificaFiltriBuilderUnitTests`.
/// </summary>
public static class NotificaFiltriSQLBuilder
{
    /// <summary>
    /// Compone il frammento di WHERE e l'insieme dei parametri che quel frammento richiede.
    ///
    /// ⚠️ Riproduce il comportamento attuale **cosi' com'e'**, difetti compresi: e' un'estrazione, non
    /// una correzione. In particolare la parola `WHERE` viene emessa **solo** dal filtro sull'anno,
    /// quindi senza anno tutti gli altri filtri producono una stringa che inizia per " AND ..." e
    /// finisce per attaccarsi alla `ON` dell'ultima LEFT JOIN della SELECT — dove non elimina righe.
    /// I test dedicati lo fissano; la correzione e' un intervento separato.
    /// </summary>
    public static NotificaFiltri Componi(NotificaFiltriInput f)
    {
        var where = string.Empty;

        if (f.AnnoValidita.HasValue)
            where += " WHERE n.year=@anno";
        if (f.MeseValidita.HasValue)
            where += " AND n.month=@mese";

        if (!f.EntiIds.IsNullNotAny())
            where += " AND internal_organization_id IN @entiIds";

        if (!f.Recapitisti.IsNullNotAny())
            where += " AND Recapitista IN @Recapitisti";

        if (!f.Consolidatori.IsNullNotAny())
            where += " AND Consolidatore IN @Consolidatori";

        var prodotto = string.IsNullOrEmpty(f.Prodotto) ? null : f.Prodotto;
        var cap = string.IsNullOrEmpty(f.Cap) ? null : f.Cap;
        var profilo = string.IsNullOrEmpty(f.Profilo) ? null : f.Profilo;
        var tipoNotifica = f.TipoNotifica ?? null;
        var contestazione = f.StatoContestazione ?? null;
        var iun = string.IsNullOrEmpty(f.Iun) ? null : f.Iun;
        var recipientId = string.IsNullOrEmpty(f.RecipientId) ? null : f.RecipientId;

        if (!string.IsNullOrEmpty(iun))
            where += " AND n.iun=@iun";

        if (!string.IsNullOrEmpty(recipientId))
            where += " AND recipient_id=@recipientId";

        if (!string.IsNullOrEmpty(prodotto))
            where += " AND c.product=@prodotto";

        if (!string.IsNullOrEmpty(cap))
            where += " AND zip_code=@cap";
        if (!string.IsNullOrEmpty(profilo))
            where += " AND e.institutionType=@profilo";

        IEnumerable<string?> tnot = [];
        if (!tipoNotifica.IsNullNotAny())
        {
            tnot = tipoNotifica!.Select(x => x.Map()).Where(x => !string.IsNullOrEmpty(x));
            // Digitali mappa sulla STRINGA VUOTA, non su un codice: viene scartata dalla lista dei
            // paper_product_type e rappresentata con l'IS NULL. Chiedendo *solo* Digitali la lista
            // resta vuota e la condizione si riduce al solo IS NULL. Sembra una svista, non lo e'.
            if (tipoNotifica!.Where(x => x == TipoNotifica.Digitali).FirstOrDefault() == TipoNotifica.Digitali)
                where += " AND (paper_product_type IN @tipoNotifica OR paper_product_type IS NULL)";
            else
                where += " AND paper_product_type IN @tipoNotifica";
        }

        // Lo stato 1 ("Non Contestata") non esiste in pfw.Contestazioni: e' il default di chi NON ha
        // una riga, quindi si traduce in IS NULL invece che in un IN.
        if (!contestazione.IsNullNotAny() && contestazione!.SequenceEqual([1]))
            where += " and t.FKIdFlagContestazione is NULL";
        else if (!contestazione.IsNullNotAny() && contestazione!.Contains(1))
            where += " and (t.FKIdFlagContestazione is NULL OR t.FKIdFlagContestazione IN @contestazione)";
        else if (!contestazione.IsNullNotAny())
            where += " and t.FKIdFlagContestazione IN @contestazione";
        else if (contestazione.IsNullNotAny())
            contestazione = null;

        var parametri = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (f.Page.HasValue)
            parametri["Page"] = f.Page.Value;
        if (f.Size.HasValue)
            parametri["Size"] = f.Size.Value;
        if (f.AnnoValidita.HasValue)
            parametri["Anno"] = f.AnnoValidita.Value;
        if (f.MeseValidita.HasValue)
            parametri["Mese"] = f.MeseValidita.Value;

        if (!string.IsNullOrEmpty(prodotto))
            parametri["Prodotto"] = prodotto;

        if (!string.IsNullOrEmpty(cap))
            parametri["Cap"] = cap;

        if (!string.IsNullOrEmpty(profilo))
            parametri["Profilo"] = profilo;

        if (!tipoNotifica.IsNullNotAny())
            parametri["TipoNotifica"] = tnot;

        // Nota fedele all'originale: il parametro c'e' anche nel ramo "solo stato 1", dove il WHERE
        // non contiene alcun @contestazione. Un parametro di troppo e' innocuo per Dapper; toglierlo
        // sarebbe un cambio di comportamento, quindi resta.
        if (contestazione != null)
            parametri["Contestazione"] = contestazione;

        if (!string.IsNullOrEmpty(iun))
            parametri["Iun"] = iun;

        if (!f.EntiIds.IsNullNotAny())
            parametri["EntiIds"] = f.EntiIds!;

        if (!f.Recapitisti.IsNullNotAny())
            parametri["Recapitisti"] = f.Recapitisti!;

        if (!f.Consolidatori.IsNullNotAny())
            parametri["Consolidatori"] = f.Consolidatori!;

        if (!string.IsNullOrEmpty(recipientId))
            parametri["RecipientId"] = recipientId;

        return new NotificaFiltri { Where = where, Parametri = parametri };
    }

    /// <summary>
    /// I quattro nomi che `NotificaQueryGetByListEntiPersistencev2` passa davvero al `SqlCommand`.
    ///
    /// ⚠️ **E' il difetto della riscrittura v2, isolato qui perche' sia visibile e verificabile.** La
    /// v1 usa Dapper, che prende l'intero insieme di parametri e per giunta espande le liste degli
    /// `IN`; la v2 costruisce il comando a mano e ne aggiunge quattro — quindi ogni altro filtro
    /// produce `Must declare the scalar variable`, e ogni filtro a lista produce `Incorrect syntax`
    /// perche' `IN @lista` e' una comodita' di Dapper, non T-SQL.
    ///
    /// Questo metodo **riproduce il comportamento attuale**, non lo corregge: serve a poterlo
    /// asserire senza database. Chi ripara la v2 fa passare l'intero `filtri.Parametri` ed espande le
    /// liste, e a quel punto i test `[Ignore]` diventano verdi.
    /// </summary>
    public static IReadOnlyList<string> NomiParametriComandoV2(NotificaFiltri filtri) =>
        new[] { "Page", "Size", "Anno", "Mese" }
            .Where(filtri.Parametri.ContainsKey)
            .ToArray();

    /// <summary>
    /// I segnaposto `@nome` presenti in un frammento SQL, senza duplicati. Serve a esprimere
    /// l'invariante come confronto fra due insiemi invece che come elenco scritto a mano, cosi' un
    /// filtro aggiunto domani entra nel test da solo.
    /// </summary>
    public static IReadOnlyCollection<string> Segnaposto(string sql) =>
        Regex.Matches(sql ?? string.Empty, "@([A-Za-z_][A-Za-z0-9_]*)")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}

/// <summary>
/// I valori su cui i filtri lavorano. Le due Query (`NotificaQueryGetByListaEnti` e `...v2`) sono
/// identiche proprieta' per proprieta': questo tipo e' il loro denominatore comune, e permette di
/// esercitare la composizione senza costruire una Query completa di `AuthenticationInfo`.
/// </summary>
public sealed class NotificaFiltriInput
{
    public int? AnnoValidita { get; init; }
    public int? MeseValidita { get; init; }
    public string? Prodotto { get; init; }
    public string? Cap { get; init; }
    public string? Profilo { get; init; }
    public TipoNotifica[]? TipoNotifica { get; init; }
    public string? Iun { get; init; }
    public int? Page { get; init; }
    public int? Size { get; init; }
    public int[]? StatoContestazione { get; init; }
    public string[]? EntiIds { get; init; }
    public string? RecipientId { get; init; }
    public string[]? Consolidatori { get; init; }
    public string[]? Recapitisti { get; init; }

    public static NotificaFiltriInput Da(NotificaQueryGetByListaEnti c) => new()
    {
        AnnoValidita = c.AnnoValidita,
        MeseValidita = c.MeseValidita,
        Prodotto = c.Prodotto,
        Cap = c.Cap,
        Profilo = c.Profilo,
        TipoNotifica = c.TipoNotifica,
        Iun = c.Iun,
        Page = c.Page,
        Size = c.Size,
        StatoContestazione = c.StatoContestazione,
        EntiIds = c.EntiIds,
        RecipientId = c.RecipientId,
        Consolidatori = c.Consolidatori,
        Recapitisti = c.Recapitisti
    };

    public static NotificaFiltriInput Da(NotificaQueryGetByListaEntiv2 c) => new()
    {
        AnnoValidita = c.AnnoValidita,
        MeseValidita = c.MeseValidita,
        Prodotto = c.Prodotto,
        Cap = c.Cap,
        Profilo = c.Profilo,
        TipoNotifica = c.TipoNotifica,
        Iun = c.Iun,
        Page = c.Page,
        Size = c.Size,
        StatoContestazione = c.StatoContestazione,
        EntiIds = c.EntiIds,
        RecipientId = c.RecipientId,
        Consolidatori = c.Consolidatori,
        Recapitisti = c.Recapitisti
    };
}

/// <summary>Frammento di WHERE e parametri che quel frammento richiede.</summary>
public sealed class NotificaFiltri
{
    public required string Where { get; init; }

    /// <summary>Confronto per nome case-insensitive, come fa SQL Server con i parametri.</summary>
    public required IReadOnlyDictionary<string, object> Parametri { get; init; }
}
