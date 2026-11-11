using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

// Query di lettura dell'Invio a SAP (PF-965). Tutte leggono da viste be.vw* (be-invio-sap.md §2.1, §8.3).

/// <summary>Griglia e "Download Risultati": <c>be.vwInvioSAPGriglia</c>.</summary>
public sealed class InvioSapGrigliaQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<InvioSapGrigliaDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int Anno { get; init; }
    public int[]? Mesi { get; init; }
    public string[]? TipologieFattura { get; init; }
    /// <summary>Codice normalizzato di <c>FiltroStatoInvioSap</c>.</summary>
    public string StatoInvio { get; init; } = "TUTTE";
}

/// <summary>Anni presenti in <c>be.vwInvioSAPGriglia</c>, dal piu' recente: valori del filtro Anno.</summary>
public sealed class InvioSapAnniQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<int>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
}

/// <summary>Mesi presenti in <c>be.vwInvioSAPGriglia</c> per un anno: valori del filtro Mese.</summary>
public sealed class InvioSapMesiQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<int>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int Anno { get; init; }
}

/// <summary>Tipologie presenti in <c>be.vwInvioSAPGriglia</c> per un anno (e i mesi, se dati): valori del filtro Tipologia.</summary>
public sealed class InvioSapTipologieQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<string>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int Anno { get; init; }
    public int[]? Mesi { get; init; }
}

/// <summary>Il <c>NumeroFatture</c> del rigo <c>DA_INVIARE</c> della chiave (D10); <c>null</c> se il rigo non c'e'.</summary>
public sealed class InvioSapRigoDaInviareQuery(IAuthenticationInfo authenticationInfo) : IRequest<int?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int Anno { get; init; }
    public int Mese { get; init; }
    public string TipologiaFattura { get; init; } = string.Empty;
}

/// <summary>Dettaglio di un rigo <c>INVIATE</c> o <c>IN_ELABORAZIONE</c>: <c>be.vwInvioSAPDettaglio</c>.</summary>
public sealed class InvioSapDettaglioQuery(IAuthenticationInfo authenticationInfo) : IRequest<InvioSapDettaglioListDto>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int Anno { get; init; }
    public int Mese { get; init; }
    public string TipologiaFattura { get; init; } = string.Empty;
    /// <summary><c>INVIATE</c> o <c>IN_ELABORAZIONE</c> (il dettaglio di <c>DA_INVIARE</c> non esiste: C1).</summary>
    public string StatoInvio { get; init; } = string.Empty;
    public string? RagioneSociale { get; init; }
    public string? NumeroFattura { get; init; }
    public DateTime? DataFattura { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}

/// <summary>Stato delle fatture scelte per il re-invio, dalla vista dettaglio.</summary>
public sealed class InvioSapStatoFattureQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<InvioSapStatoFatturaDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public IReadOnlyList<long> IdFatture { get; init; } = [];
}

/// <summary>"Seleziona tutto": quante fatture ha il rigo nello stato richiesto, tolte le escluse (vista dettaglio).</summary>
public sealed class InvioSapConteggioSelezioneTotaleQuery(IAuthenticationInfo authenticationInfo) : IRequest<int>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int Anno { get; init; }
    public int Mese { get; init; }
    public string TipologiaFattura { get; init; } = string.Empty;
    public IReadOnlyList<long> IdFattureEscluse { get; init; } = [];
    /// <summary><c>true</c> = fatture inviate (re-invio), <c>false</c> = fatture da inviare (primo invio).</summary>
    public bool Inviata { get; init; } = true;
}

/// <summary>Download delle re-inviate: <c>be.vwInvioSAPReportFattureReInviate</c>.</summary>
public sealed class InvioSapReinviateQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<InvioSapReinviataDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int Anno { get; init; }
    public int[]? Mesi { get; init; }
    public string[]? TipologieFattura { get; init; }
}

/// <summary>Lock attivo dell'azione "Genera JSON": <c>be.vwLockAttiviAzioniPF</c>; <c>null</c> se non c'e'.</summary>
public sealed class InvioSapLockQuery(IAuthenticationInfo authenticationInfo) : IRequest<InvioSapLockDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
}
