using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;

/// <summary>
/// Mapping request → query/command e dto/esito → response dell'Invio a SAP. Qui vive tutto cio' che
/// altrimenti allungherebbe i metodi endpoint oltre le 3-4 righe. Un filtro non valido lancia
/// <see cref="ValidationException"/>, che il gestore globale traduce in 400.
/// </summary>
public static class InvioSapExtensions
{
    public const string MimeExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static int AnnoCorrente => DateTime.UtcNow.ItalianTime().Year;

    private static void Lancia(IReadOnlyList<string> errori)
    {
        if (errori.Count > 0) throw new ValidationException(string.Join("; ", errori));
    }

    private static string[]? Pulisci(IEnumerable<string>? valori) =>
        valori?.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).Distinct().ToArray() is { Length: > 0 } a ? a : null;

    public static InvioSapGrigliaQuery Map(this InvioSapGrigliaRequest? request, IAuthenticationInfo authInfo)
    {
        request ??= new();
        Lancia(InvioSapRegole.ValidaFiltriGriglia(request.Anno, request.Mesi, request.TipologieFattura, request.StatoInvio, AnnoCorrente));
        return new InvioSapGrigliaQuery(authInfo)
        {
            Anno = request.Anno!.Value,
            Mesi = request.Mesi is { Count: > 0 } ? request.Mesi.Distinct().ToArray() : null,
            TipologieFattura = Pulisci(request.TipologieFattura),
            StatoInvio = FiltroStatoInvioSap.Normalizza(request.StatoInvio)
        };
    }

    public static InvioSapReinviateQuery Map(this InvioSapReinviateRequest? request, IAuthenticationInfo authInfo)
    {
        request ??= new();
        Lancia(InvioSapRegole.ValidaFiltriGriglia(request.Anno, request.Mesi, request.TipologieFattura, null, AnnoCorrente));
        return new InvioSapReinviateQuery(authInfo)
        {
            Anno = request.Anno!.Value,
            Mesi = request.Mesi is { Count: > 0 } ? request.Mesi.Distinct().ToArray() : null,
            TipologieFattura = Pulisci(request.TipologieFattura)
        };
    }

    public static InvioSapDettaglioQuery Map(this InvioSapDettaglioRequest? request, IAuthenticationInfo authInfo)
    {
        request ??= new();
        Lancia(InvioSapRegole.ValidaFiltriDettaglio(request.AnnoRiferimento, request.MeseRiferimento, request.TipologiaFattura,
            request.StatoInvio, request.RagioneSociale, request.NumeroFattura, request.Page, request.PageSize));
        return new InvioSapDettaglioQuery(authInfo)
        {
            Anno = request.AnnoRiferimento!.Value,
            Mese = request.MeseRiferimento!.Value,
            TipologiaFattura = request.TipologiaFattura!.Trim(),
            StatoInvio = FiltroStatoInvioSap.Normalizza(request.StatoInvio),
            RagioneSociale = request.RagioneSociale,
            NumeroFattura = request.NumeroFattura,
            DataFattura = request.DataFattura,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public static InvioSapGeneraCommand Map(this InvioSapGeneraRequest? request, IAuthenticationInfo authInfo)
    {
        request ??= new();
        return new InvioSapGeneraCommand(authInfo)
        {
            Anno = request.Anno,
            Mese = request.Mese,
            TipologiaFattura = request.TipologiaFattura,
            IdFatture = request.IdFatture,
            Nota = request.Nota,
            NumeroDocumentoStorno = request.NumeroDocumentoStorno
        };
    }

    public static IReadOnlyList<InvioSapGrigliaResponse> Map(this IEnumerable<InvioSapGrigliaDto> righe) =>
        righe.Select(r => new InvioSapGrigliaResponse
        {
            Id = $"{r.TipologiaFattura}|{r.Anno}|{r.Mese}|{r.StatoInvio}",
            TipologiaFattura = r.TipologiaFattura,
            AnnoRiferimento = r.Anno,
            MeseRiferimento = r.Mese,
            StatoInvio = r.StatoInvio,
            NumeroFatture = r.NumeroFatture,
            ImportoTotale = Math.Round((decimal)(r.ImportoTotale ?? 0), 2)
        }).ToList();

    public static InvioSapDettaglioResponse Map(this InvioSapDettaglioListDto lista) => new()
    {
        Count = lista.Count,
        Fatture = lista.Fatture.Select(f =>
        {
            var note = InvioSapNota.ParseNoteInvio(f.NoteInvio);
            return new InvioSapFatturaResponse
            {
                IdFattura = f.IdFattura,
                NumeroFattura = f.IdentificativoFattura,
                Progressivo = f.Progressivo,
                TipoDocumento = f.FkIdTipoDocumento,
                TipologiaFattura = f.FkTipologiaFattura,
                IdEnte = f.FkIdEnte,
                RagioneSociale = f.RagioneSociale,
                Importo = Math.Round((decimal)f.TotaleFattura, 2),
                IdDocumento = string.IsNullOrWhiteSpace(f.IdDocumento) ? null : f.IdDocumento,
                DataFattura = f.DataFattura,
                StatoInvio = StatoInvioSap.DaFatturaInviata(f.FatturaInviata),
                Semestre = f.Semestre,
                DataUltimaGenerazione = f.DataUltimaGenerazione?.ItalianTime(),
                NomeFile = f.NomeFile,
                NumeroInvii = note.Count,
                StoricoInvii = note.Select((n, i) => new InvioSapStoricoInvioResponse
                {
                    NInvio = i + 1,
                    Data = n.Data?.ItalianTime(),
                    Testo = n.Testo,
                    Azione = n.Azione
                }).ToList()
            };
        }).ToList()
    };

    public static InvioSapLockResponse Map(this InvioSapLockDto dto) => new()
    {
        Azione = dto.Azione,
        BatchId = dto.BatchId,
        LockStart = dto.LockStartUTC.ItalianTime(),
        LockTimeout = dto.LockTimeoutUTC.ItalianTime()
    };

    public static IReadOnlyList<InvioSapStatoResponse> Stati() =>
        FiltroStatoInvioSap.Descrizioni.Select(d => new InvioSapStatoResponse { Codice = d.Codice, Descrizione = d.Descrizione }).ToList();

    /// <summary>Esito del Genera → HTTP (be-invio-sap.md §11).</summary>
    public static IResult ToResult(this InvioSapGeneraEsito esito) => esito.Tipo switch
    {
        InvioSapGeneraEsitoTipo.Ok => Results.Accepted(value: new InvioSapGeneraResponse
        {
            BatchId = esito.BatchId!.Value,
            Stato = StatoBatchSap.IN_ELABORAZIONE,
            DataAvvio = esito.DataAvvioUtc!.Value.ItalianTime(),
            NumeroFatture = esito.NumeroFatture!.Value,
            IdFatture = esito.IdFatture
        }),
        InvioSapGeneraEsitoTipo.NonValida => Results.BadRequest(string.Join("; ", esito.Errori)),
        InvioSapGeneraEsitoTipo.NonTrovate => Results.NotFound(),
        InvioSapGeneraEsitoTipo.Conflitto => Results.Conflict(new InvioSapConflittoResponse
        {
            Motivo = esito.Motivo ?? InvioSapMotivoConflitto.LOCK_ATTIVO,
            BatchId = esito.LockBatchId,
            LockTimeout = esito.LockTimeoutUtc?.ItalianTime()
        }),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError,
            detail: "Pipeline di generazione non avviata: nessuna modifica salvata, riprovare.")
    };

    public static IResult ToExcel<T>(this IEnumerable<T> righe, string foglio, string prefissoFile)
    {
        var content = righe.FillOneSheetv2(foglio).ToExcel();
        var nome = $"{prefissoFile}_{DateTime.UtcNow.ItalianTime():yyyyMMddHHmm}.xlsx";
        var result = new DisposableStreamResult(content, MimeExcel) { FileDownloadName = nome };
        return Results.Stream(result.FileStream, result.ContentType, result.FileDownloadName);
    }
}
