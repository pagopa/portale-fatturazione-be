using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Helper per testare TUTTE le combinazioni possibili di un insieme di filtri (2^n sottoinsiemi).
/// Per ogni sottoinsieme verifica due invarianti complementari sul risultato:
///   - AND: ogni riga restituita rispetta TUTTI i filtri attivi (nessuna inclusione errata);
///   - TARGET: una riga seed che soddisfa ogni filtro (impostato sui suoi stessi valori) e' SEMPRE
///     presente (nessuna esclusione errata / filtro troppo restrittivo).
/// Insieme provano che ciascun filtro — e ogni loro combinazione in AND — restringe correttamente.
/// </summary>
internal static class FilterCombinations
{
    public static async Task AssertAllSubsets<TQuery>(
        (string Name, Action<TQuery> Apply, Func<SimpleGestioneFattureDto, bool> Match)[] filters,
        Func<TQuery> newQuery,
        Func<TQuery, Task<GestioneFattureListDto?>> send,
        Func<SimpleGestioneFattureDto, bool> isTarget)
    {
        int n = filters.Length;
        var failures = new List<string>();

        for (int mask = 0; mask < (1 << n); mask++)
        {
            var q = newQuery();
            var activeNames = new List<string>();
            for (int i = 0; i < n; i++)
                if ((mask & (1 << i)) != 0)
                {
                    filters[i].Apply(q);
                    activeNames.Add(filters[i].Name);
                }

            var res = await send(q);
            var rows = res?.GestioneFatture?.ToList() ?? new List<SimpleGestioneFattureDto>();
            var combo = activeNames.Count == 0 ? "(nessun filtro)" : string.Join("+", activeNames);

            // AND: ogni filtro attivo deve valere su tutte le righe restituite.
            for (int i = 0; i < n; i++)
                if ((mask & (1 << i)) != 0 && !rows.All(filters[i].Match))
                    failures.Add($"[{combo}] filtro '{filters[i].Name}' violato: righe non conformi restituite.");

            // TARGET: la riga seed (che soddisfa ogni filtro impostato sui suoi valori) deve esserci sempre.
            if (!rows.Any(isTarget))
                failures.Add($"[{combo}] riga target attesa assente: filtro troppo restrittivo.");
        }

        Assert.That(failures, Is.Empty,
            $"Combinazioni di filtri fallite ({failures.Count}):\n{string.Join("\n", failures)}");
    }
}
