using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;

/// <summary>
/// Nota di un invio: colonna <c>Nota</c> (tipo <c>json</c>) di <c>pfw.DettaglioInvioSAP</c>, formato di DATA
/// <c>{"Data","Testo","Azione"}</c>. Le viste la restituiscono aggregata in <c>NoteInvio</c>/<c>Note</c> come
/// <c>[{"Nota":{...}},...]</c> (FOR JSON PATH, ordinata per batch).
/// In DEV la data compare in due formati (C7): <c>2026-10-27 17:21:40.147</c> e ISO a 7 decimali.
/// </summary>
public sealed record InvioSapNota(DateTime? Data, string? Testo, string? Azione)
{
    private static readonly string[] FormatiData =
    [
        "yyyy-MM-dd HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ",
        "yyyy-MM-ddTHH:mm:ssZ"
    ];

    private static readonly JsonSerializerOptions OpzioniScrittura = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    /// <summary>JSON da scrivere nella colonna: data in ISO UTC, come le righe recenti di DATA.</summary>
    public string ToJson() => JsonSerializer.Serialize(new Dictionary<string, string?>
    {
        ["Data"] = Data?.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture),
        ["Testo"] = Testo,
        ["Azione"] = Azione
    }, OpzioniScrittura);

    /// <summary>
    /// Legge l'aggregato delle viste. Non lancia mai: JSON nullo, vuoto o malformato danno una lista vuota,
    /// un elemento illeggibile viene saltato, un campo mancante resta <c>null</c>.
    /// </summary>
    public static IReadOnlyList<InvioSapNota> ParseNoteInvio(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];
            var note = new List<InvioSapNota>();
            foreach (var elemento in doc.RootElement.EnumerateArray())
            {
                var nota = elemento.ValueKind == JsonValueKind.Object && elemento.TryGetProperty("Nota", out var interna)
                    ? interna
                    : elemento;
                if (nota.ValueKind == JsonValueKind.String)
                {
                    // Nota salvata come stringa JSON (testo): si prova a rileggerla.
                    var testo = nota.GetString();
                    if (string.IsNullOrWhiteSpace(testo)) continue;
                    try
                    {
                        using var annidato = JsonDocument.Parse(testo);
                        note.Add(DaOggetto(annidato.RootElement));
                    }
                    catch (JsonException) { }
                    continue;
                }
                if (nota.ValueKind != JsonValueKind.Object) continue;
                note.Add(DaOggetto(nota));
            }
            return note;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static InvioSapNota DaOggetto(JsonElement nota) =>
        new(LeggiData(Stringa(nota, "Data")), Stringa(nota, "Testo"), Stringa(nota, "Azione"));

    private static string? Stringa(JsonElement oggetto, string nome) =>
        oggetto.ValueKind == JsonValueKind.Object
        && oggetto.TryGetProperty(nome, out var valore)
        && valore.ValueKind == JsonValueKind.String
            ? valore.GetString()
            : null;

    public static DateTime? LeggiData(string? testo)
    {
        if (string.IsNullOrWhiteSpace(testo)) return null;
        return DateTime.TryParseExact(testo.Trim(), FormatiData, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var data)
            ? DateTime.SpecifyKind(data, DateTimeKind.Utc)
            : null;
    }
}
