using PortaleFatture.BE.Core.Entities.SEND.Fatture.InvioSap;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

// DTO dell'Invio a SAP (PF-965, figlio di PF-735). Ognuno rispecchia le colonne di una vista di DATA
// (be-invio-sap.md §5.3): i nomi delle proprieta' coincidono con gli alias del builder.

/// <summary>Riga di <c>be.vwInvioSAPGriglia</c>: un rigo per Tipologia × Anno × Mese × Stato.</summary>
public sealed class InvioSapGrigliaDto
{
    public string TipologiaFattura { get; set; } = string.Empty;
    /// <summary><c>FatturaInviata</c>: 0 da inviare, 1 inviata, NULL in elaborazione.</summary>
    public bool? StatoFattura { get; set; }
    public int NumeroFatture { get; set; }
    public int Anno { get; set; }
    public int Mese { get; set; }
    public double? ImportoTotale { get; set; }

    public string StatoInvio => StatoInvioSap.DaFatturaInviata(StatoFattura);
}

/// <summary>Riga di <c>be.vwInvioSAPDettaglio</c>: una fattura che ha almeno un invio (C1).</summary>
public sealed class InvioSapDettaglioDto
{
    public long IdFattura { get; set; }
    public string? FkIdTipoDocumento { get; set; }
    public string? FkTipologiaFattura { get; set; }
    public string? FkIdEnte { get; set; }
    public string? RagioneSociale { get; set; }
    public DateTime DataFattura { get; set; }
    public string? IdentificativoFattura { get; set; }
    public double TotaleFattura { get; set; }
    public int AnnoRiferimento { get; set; }
    public int MeseRiferimento { get; set; }
    public string? IdDocumento { get; set; }
    public long? Progressivo { get; set; }
    public bool? FatturaInviata { get; set; }
    public string? Semestre { get; set; }
    public DateTime? DataUltimaGenerazione { get; set; }
    public string? NomeFile { get; set; }
    public string? NoteInvio { get; set; }
}

public sealed class InvioSapDettaglioListDto
{
    public IEnumerable<InvioSapDettaglioDto> Fatture { get; set; } = [];
    public int Count { get; set; }
}

/// <summary>Riga di <c>be.vwInvioSAPReportFattureReInviate</c>: fatture con piu' di un batch COMPLETATO.</summary>
public sealed class InvioSapReinviataDto
{
    public long IdFattura { get; set; }
    public string? TipoDocumento { get; set; }
    public string? TipologiaFattura { get; set; }
    public string? IdEnte { get; set; }
    public string? RagioneSociale { get; set; }
    public DateTime DataFattura { get; set; }
    public decimal TotaleFattura { get; set; }
    public int AnnoRiferimento { get; set; }
    public int MeseRiferimento { get; set; }
    public long? Progressivo { get; set; }
    public int ConteggioInvio { get; set; }
    public string? Note { get; set; }
}

/// <summary>Riga di <c>be.vwLockAttiviAzioniPF</c> (orari in UTC).</summary>
public sealed class InvioSapLockDto
{
    public Guid IdAzione { get; set; }
    public string? Azione { get; set; }
    public DateTime LockStartUTC { get; set; }
    public DateTime LockTimeoutUTC { get; set; }
    public int? BatchId { get; set; }
}

/// <summary>Stato di una fattura per il controllo del re-invio, letto dalla vista dettaglio.</summary>
public sealed class InvioSapStatoFatturaDto
{
    public long IdFattura { get; set; }
    public int AnnoRiferimento { get; set; }
    public int MeseRiferimento { get; set; }
    public string FkTipologiaFattura { get; set; } = string.Empty;
    public bool? FatturaInviata { get; set; }

    public InvioSapFatturaCorrente ToCorrente() =>
        new(IdFattura, AnnoRiferimento, MeseRiferimento, FkTipologiaFattura, FatturaInviata);
}

/// <summary>Foglio "Download Risultati": una riga per rigo della griglia (la vista e' aggregata).</summary>
public sealed class InvioSapGrigliaExcelDto
{
    [HeaderAttributev2(caption: "Tipologia Fattura", Order = 1)]
    public string? TipologiaFattura { get; set; }

    [HeaderAttributev2(caption: "Anno", Order = 2)]
    public int Anno { get; set; }

    [HeaderAttributev2(caption: "Mese", Order = 3)]
    public string? Mese { get; set; }

    [HeaderAttributev2(caption: "Stato", Order = 4)]
    public string? Stato { get; set; }

    [HeaderAttributev2(caption: "Numero Fatture", Order = 5)]
    public int NumeroFatture { get; set; }

    [HeaderAttributev2(caption: "Importo Totale", Order = 6)]
    public decimal ImportoTotale { get; set; }

    public static InvioSapGrigliaExcelDto Da(InvioSapGrigliaDto riga) => new()
    {
        TipologiaFattura = riga.TipologiaFattura,
        Anno = riga.Anno,
        Mese = riga.Mese.GetMonth(),
        Stato = riga.StatoInvio,
        NumeroFatture = riga.NumeroFatture,
        ImportoTotale = Math.Round((decimal)(riga.ImportoTotale ?? 0), 2)
    };
}

/// <summary>Foglio "Fatture re-inviate": una riga per fattura, note in testo semplice.</summary>
public sealed class InvioSapReinviataExcelDto
{
    [HeaderAttributev2(caption: "Id Fattura", Order = 1)]
    public long IdFattura { get; set; }

    [HeaderAttributev2(caption: "Progressivo", Order = 2)]
    public long? Progressivo { get; set; }

    [HeaderAttributev2(caption: "Tipo Documento", Order = 3)]
    public string? TipoDocumento { get; set; }

    [HeaderAttributev2(caption: "Tipologia Fattura", Order = 4)]
    public string? TipologiaFattura { get; set; }

    [HeaderAttributev2(caption: "Id Ente", Order = 5)]
    public string? IdEnte { get; set; }

    [HeaderAttributev2(caption: "Ragione Sociale", Order = 6)]
    public string? RagioneSociale { get; set; }

    [HeaderAttributev2(caption: "Data Fattura", Order = 7)]
    public string? DataFattura { get; set; }

    [HeaderAttributev2(caption: "Totale Fattura", Order = 8)]
    public decimal TotaleFattura { get; set; }

    [HeaderAttributev2(caption: "Anno", Order = 9)]
    public int AnnoRiferimento { get; set; }

    [HeaderAttributev2(caption: "Mese", Order = 10)]
    public string? MeseRiferimento { get; set; }

    [HeaderAttributev2(caption: "Numero Invii", Order = 11)]
    public int ConteggioInvio { get; set; }

    [HeaderAttributev2(caption: "Note", Order = 12)]
    public string? Note { get; set; }

    public static InvioSapReinviataExcelDto Da(InvioSapReinviataDto riga) => new()
    {
        IdFattura = riga.IdFattura,
        Progressivo = riga.Progressivo,
        TipoDocumento = riga.TipoDocumento,
        TipologiaFattura = riga.TipologiaFattura,
        IdEnte = riga.IdEnte,
        RagioneSociale = riga.RagioneSociale,
        DataFattura = riga.DataFattura.ToString("dd/MM/yyyy"),
        TotaleFattura = riga.TotaleFattura,
        AnnoRiferimento = riga.AnnoRiferimento,
        MeseRiferimento = riga.MeseRiferimento.GetMonth(),
        ConteggioInvio = riga.ConteggioInvio,
        Note = string.Join(Environment.NewLine, InvioSapNota.ParseNoteInvio(riga.Note)
            .Select(n => $"{n.Data?.ItalianTime():dd/MM/yyyy HH:mm} {n.Testo}".Trim()))
    };
}
