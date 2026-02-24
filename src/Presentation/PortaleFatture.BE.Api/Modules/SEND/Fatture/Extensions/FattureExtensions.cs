using System.Data;
using System.IO.Compression;
using System.Reflection;
using DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Api.Modules.Fatture;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Messaggi;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Entities.SEND.Fatture;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands;
using PortaleFatture.BE.Infrastructure.Gateway.Storage;
namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;

/// <summary>
/// Enum per specificare lo scope del documento contabile
/// </summary>
public enum DocContabileScope
{
    Emesso,
    Sospeso
}

/// <summary>
/// Estensioni per la mappatura delle fatture e dei documenti contabili
/// </summary>
public static class FattureExtensions
{
    /// <summary>
    /// Mappa un oggetto FattureDocContabiliDtoList in un DocContabileBaseResponse in base allo scope specificato.
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="scope"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static DocContabileBaseResponse Map(this FattureDocContabiliDtoList dto, DocContabileScope scope)
    {
        return scope switch
        {
            DocContabileScope.Sospeso => new DocContabileSospesoResponse
            {
                ImportoSospeso = dto.ImportoSospeso,
                Dettagli = dto.Dettagli?.Select(x => x.ToDettaglioResponse(DocContabileScope.Sospeso))
            },
            DocContabileScope.Emesso => new DocContabileEmessoResponse
            {
                Totale = dto.Importo,
                Dettagli = dto.Dettagli?.Select(x => x.ToDettaglioResponse(DocContabileScope.Emesso))
            },
            _ => throw new ArgumentException($"Scope non supportato: {scope}", nameof(scope))
        };
    }

    private static DocContabileDettaglioResponse ToDettaglioResponse(this FatturaDocContabileDto x, DocContabileScope scope = DocContabileScope.Emesso)
    {
        return new DocContabileDettaglioResponse
        {
            Fattura = x.ToFatturaResponse(scope)
        };
    }

    private static DocContabileFatturaResponse ToFatturaResponse(this FatturaDocContabileDto x, DocContabileScope scope)
    {
        return new DocContabileFatturaResponse
        {
            Totale = scope == DocContabileScope.Emesso ? x.TotaleFattura : x.ImportoSospesoParziale,
            Progressivo = x.Progressivo,
            IdFattura = x.IdFattura,
            DataFattura = x.DataFattura,
            Prodotto = x.Prodotto,
            PeriodoFatturazione = x.PeriodoFatturazione,
            IstitutioId = x.IstitutioId,
            OnboardingTokenId = x.OnboardingTokenId,
            RagioneSociale = x.RagioneSociale,
            TipoContratto = x.TipoContratto,
            IdContratto = x.IdContratto,
            TipoDocumento = x.TipoDocumento,
            Divisa = x.Divisa,
            MetodoPagamento = x.MetodoPagamento,
            CausaleFattura = x.CausaleFattura,
            SplitPayment = x.SplitPayment,
            Inviata = x.Inviata,
            Sollecito = x.Sollecito,
            Stato = x.Stato,
            DatiGeneraliDocumento =
            [
                new DocContabileDatiGeneraliResponse
                {
                    TipologiaFattura = x.TipologiaFattura,
                    RiferimentoNumeroLinea = x.RiferimentoNumeroLinea,
                    IdDocumento = x.IdDocumento,
                    DataDocumento = x.DataDocumento.ToString("yyyy-MM-dd"),
                    NumItem = x.NumItem,
                    CodiceCommessaConvenzione = x.CodiceCommessaConvenzione,
                    Cup = x.Cup,
                    Cig = x.Cig
                }
            ],
            Posizioni = x.Posizioni?.Select(z => z.ToPosizioneResponse())
        };
    }

    private static DocContabilePosizioneResponse ToPosizioneResponse(this FatturaDocContabilePosizioniDto z)
    {
        return new DocContabilePosizioneResponse
        {
            NumeroLinea = z.NumeroLinea,
            Testo = z.Testo,
            CodiceMateriale = z.CodiceMateriale,
            Quantita = z.Quantita,
            PrezzoUnitario = z.PrezzoUnitario,
            Imponibile = z.Imponibile,
            PeriodoRiferimento = z.PeriodoRiferimento
        };
    }

    public static DocContabileEliminateResponse Map(this FattureDocContabiliEliminateDtoList dto)
    {
        return new DocContabileEliminateResponse
        {
            Totale = dto.Importo,
            Dettagli = dto.Dettagli?.Select(x => x.ToEliminataDettaglioResponse())
        };
    }

    private static DocContabileEliminataDettaglioResponse ToEliminataDettaglioResponse(this FatturaDocContabileEliminataDto x)
    {
        return new DocContabileEliminataDettaglioResponse
        {
            Fattura = x.ToEliminataFatturaResponse()
        };
    }

    private static DocContabileEliminataFatturaResponse ToEliminataFatturaResponse(this FatturaDocContabileEliminataDto x)
    {
        return new DocContabileEliminataFatturaResponse
        {
            Totale = x.TotaleFattura,
            Progressivo = x.Progressivo,
            IdFattura = x.IdFattura,
            DataFattura = x.DataFattura,
            Prodotto = x.Prodotto,
            PeriodoFatturazione = x.PeriodoFatturazione,
            IstitutioId = x.IstitutioId,
            OnboardingTokenId = x.OnboardingTokenId,
            RagioneSociale = x.RagioneSociale,
            IdContratto = x.IdContratto,
            TipoDocumento = x.TipoDocumento,
            TipoContratto = x.TipoContratto,
            Divisa = x.Divisa,
            MetodoPagamento = x.MetodoPagamento,
            CausaleFattura = x.CausaleFattura,
            SplitPayment = x.SplitPayment,
            Inviata = x.Inviata,
            Sollecito = x.Sollecito,
            Stato = x.Stato,
            DatiGeneraliDocumento =
            [
                new DocContabileDatiGeneraliResponse
                {
                    TipologiaFattura = x.TipologiaFattura,
                    RiferimentoNumeroLinea = x.RiferimentoNumeroLinea,
                    IdDocumento = x.IdDocumento,
                    DataDocumento = x.DataDocumento.ToString("yyyy-MM-dd"),
                    NumItem = x.NumItem,
                    CodiceCommessaConvenzione = x.CodiceCommessaConvenzione,
                    Cup = x.Cup,
                    Cig = x.Cig
                }
            ]
        };
    }


    public static FatturePeriodoReponse Map(this FatturePeriodoDto dto)
    {
        return new FatturePeriodoReponse()
        {
            Anno = dto.Anno,
            Mese = dto.Mese,
            DataFattura = dto.DataFattura.HasValue ? DateOnly.FromDateTime(dto.DataFattura.Value) : null,
            TipologiaFattura = dto.TipologiaFattura
        };
    }


    public static FattureDateQueryRicerca Map2(this FatturaRicercaRequest req, AuthenticationInfo authInfo)
    {
        return new FattureDateQueryRicerca(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = req.IdEnti,
            TipologiaFattura = req.TipologiaFattura,
            Cancellata = req.Cancellata == null ? false : req.Cancellata.Value
        };
    }

    public static FattureCreditoSospesoQuery Map(this FattureCreditoSospesoRicercaEnteRequest req, AuthenticationInfo authInfo)
    {
        return new FattureCreditoSospesoQuery(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            TipologiaFattura = req.TipologiaFattura,
            DateFattura = req.DateFatture
        };
    }

    public static FattureEmesseQuery Map(this FattureEmesseRicercaEnteRequest req, AuthenticationInfo authInfo)
    {
        return new FattureEmesseQuery(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            TipologiaFattura = req.TipologiaFattura,
            DateFattura = req.DateFatture
        };
    }

    public static FattureEliminateQuery Map(this FattureEliminateRicercaEnteRequest req, AuthenticationInfo authInfo)
    {
        return new FattureEliminateQuery(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            TipologiaFattura = req.TipologiaFattura,
            DateFattura = req.DateFatture
        };
    }

    public static FattureDocContabileDettaglioQuery Map(this FattureDocContabileEnteRequest req, AuthenticationInfo authInfo)
    {
        return new FattureDocContabileDettaglioQuery(authInfo)
        {
            IdFattura = req.IdFattura
        };
    }

    public static FattureDocContabileDettaglioEmessoQuery MapEmesso(this FattureDocContabileEnteRequest req, AuthenticationInfo authInfo)
    {
        return new FattureDocContabileDettaglioEmessoQuery(authInfo)
        {
            IdFattura = req.IdFattura
        };
    }

    public static DocumentoContabileDettaglioResponse Map(this FattureDocContabiliDettaglioDto dto)
    {
        return new DocumentoContabileDettaglioResponse
        {
            IdTestata = dto.IdFattura.ToString() ?? string.Empty,
            RagioneSociale = dto.RagioneSociale,
            IdDocumento = dto.IdDocumento,
            DataDocumento = dto.DataFattura,
            IdEnte = dto.IdEnte,
            Cup = dto.Cup,
            IdContratto = dto.IdContratto,
            Anno = dto.Anno.ToString(),
            Mese = dto.Mese.ToString(),
            TipologiaFattura = dto.TipologiaFattura,
            TotaleAnalogico = dto.RelTotaleAnalogico,
            TotaleDigitale = dto.RelTotaleDigitale,
            TotaleNotificheAnalogiche = dto.RelTotaleNotificheAnalogiche,
            TotaleNotificheDigitali = dto.RelTotaleNotificheDigitali,
            Totale = dto.RelTotaleNotifiche,
            TotaleFattura = dto.TotaleFatturaImponibile,
            TotaleAnalogicoIva = dto.RelTotaleIvatoAnalogico,
            TotaleDigitaleIva = dto.RelTotaleIvatoDigitale,
            Iva = dto.Iva,
            TotaleIva = dto.RelTotaleIvato,
            Caricata = dto.Caricata.HasValue && dto.Caricata.Value == true ? 1 : 0,
            TipologiaContratto = dto.TipologiaContratto,
            AnticipoDigitale = dto.AnticipoDigitale,
            AnticipoAnalogico = dto.AnticipoAnalogico,
            AccontoDigitale = dto.AccontoDigitale,
            AccontoAnalogico = dto.AccontoAnalogico,
            StornoAnalogico = dto.StornoAnalogico,
            StornoDigitale = dto.StornoDigitale
        };
    }

    public static DocumentoContabileSospeso MapToPdf(this FattureDocContabiliDettaglioDto dto)
    {
        var anticipoAnalogico = dto.AnticipoAnalogico ?? 0;
        var anticipoDigitale = dto.AnticipoDigitale ?? 0;
        var totaleAnticipo = anticipoAnalogico + anticipoDigitale;
        var stornoAnalogico = dto.StornoAnalogico ?? 0;
        var stornoDigitale = dto.StornoDigitale ?? 0;
        var totaleStorno = stornoAnalogico + stornoDigitale;
        var totaleImponibile = dto.RelTotale;

        return new DocumentoContabileSospeso
        {
            RagioneSociale = dto.RagioneSociale,
            IdContratto = dto.IdContratto ?? string.Empty,
            Anno = dto.Anno ?? 0,
            Mese = dto.Mese ?? 0,
            TotaleAnalogico = dto.RelTotaleAnalogico,
            TipologiaFattura = dto.TipologiaContratto,
            AnticipoAnalogico = anticipoAnalogico,
            AnticipoDigitale = anticipoDigitale,
            ImportoSottoSoglia = totaleImponibile - totaleAnticipo,
            TotaleDigitale = dto.RelTotaleDigitale,
            TotaleNotificheAnalogiche = dto.RelTotaleNotificheAnalogiche,
            TotaleNotificheDigitali = dto.RelTotaleNotificheDigitali,
            TotaleAnticipo = totaleAnticipo,
            StornoAnalogico = stornoAnalogico,
            StornoDigitale = stornoDigitale,
            TotaleStorno = totaleStorno,
            Totale = totaleImponibile,
        };
    }

    public static DocumentoContabileEmesso MapToPdf(this FatturaDocContabileEmessoDettaglioDto dto)
    {
        var stornoAnalogico = dto.StornoAnalogico ?? 0;
        var stornoDigitale = dto.StornoDigitale ?? 0;
        var totaleStorno = stornoAnalogico + stornoDigitale;

        return new DocumentoContabileEmesso
        {
            RagioneSociale = dto.RagioneSociale,
            IdContratto = dto.IdContratto ?? string.Empty,
            Anno = dto.Anno?.ToString() ?? string.Empty,
            Mese = dto.Mese?.ToString() ?? string.Empty,
            Totale = dto.RelTotale,
            TotaleAnalogico = dto.RelTotaleAnalogico,
            TotaleDigitale = dto.RelTotaleDigitale,
            TotaleNotificheDigitali = dto.RelTotaleNotificheDigitali,
            TotaleNotificheAnalogiche = dto.RelTotaleNotificheAnalogiche,
            TotaleAnticipo = (dto.AnticipoDigitale ?? 0) + (dto.AnticipoAnalogico ?? 0),
            AnticipoDigitale = dto.AnticipoDigitale ?? 0,
            AnticipoAnalogico = dto.AnticipoAnalogico ?? 0,
            TotaleStorno = totaleStorno,
            StornoDigitale = stornoDigitale,
            StornoAnalogico = stornoAnalogico,
            TotaleAcconto = (dto.AccontoDigitale ?? 0) + (dto.AccontoAnalogico ?? 0),
            AccontoDigitale = dto.AccontoDigitale ?? 0,
            AccontoAnalogico = dto.AccontoAnalogico ?? 0,
            Imponibile = dto.TotaleFatturaImponibile ?? 0,
            TipologiaFattura = dto.TipologiaContratto
        };
    }

    public static DocumentoContabileEmessoRiepilogo MapToPdfRiepilogo(this FatturaDocContabileEmessoDettaglioDto dto)
    {
        string mese;
        string anno;

        if (dto.MeseSospesa.HasValue && dto.AnnoSospesa.HasValue)
        {
            mese = dto.MeseSospesa.Value.ToString();
            anno = dto.AnnoSospesa.Value.ToString();
        }
        else if (DateTime.TryParse(dto.DataFatturaSospesa, out var dataSospesa))
        {
            mese = dataSospesa.Month.ToString();
            anno = dataSospesa.Year.ToString();
        }
        else
        {
            mese = dto.Mese?.ToString() ?? string.Empty;
            anno = dto.Anno?.ToString() ?? string.Empty;
        }

        return new DocumentoContabileEmessoRiepilogo
        {
            Anno = anno,
            Mese = mese,
            Totale = dto.TotaleFatturaSospesaImponibile ?? dto.RelTotale,
            TotaleDigitale = dto.RelTotaleDigitaleSospeso ?? dto.RelTotaleDigitale,
            TotaleAnalogico = dto.RelTotaleAnalogicoSospeso ?? dto.RelTotaleAnalogico,
            TotaleNotificheDigitali = dto.RelTotaleNotificheDigitaliSospeso ?? dto.RelTotaleNotificheDigitali,
            TotaleNotificheAnalogiche = dto.RelTotaleNotificheAnalogicheSospeso ?? dto.RelTotaleNotificheAnalogiche
        };
    }

    public static DocumentoContabileEmessiMultipli MapToPdfMultiplo(this IEnumerable<FatturaDocContabileEmessoDettaglioDto> dtos)
{
    var first = dtos.FirstOrDefault();
    if (first == null) return new DocumentoContabileEmessiMultipli();

    var result = new DocumentoContabileEmessiMultipli
    {
        RagioneSociale = first.RagioneSociale,
        IdContratto = first.IdContratto,
        TipologiaFattura = first.TipologiaContratto,
        DettaglioFatture = dtos.Select(d => d.MapToPdfRiepilogo()).ToList()
    };

    result.TotaleAnalogico = dtos.Sum(d => d.RelTotaleAnalogicoSospeso ?? 0);
    result.TotaleDigitale = dtos.Sum(d => d.RelTotaleDigitaleSospeso ?? 0);
    result.TotaleNotificheAnalogiche = dtos.Sum(d => d.RelTotaleNotificheAnalogicheSospeso ?? 0);
    result.TotaleNotificheDigitali = dtos.Sum(d => d.RelTotaleNotificheDigitaliSospeso ?? 0);
    result.AnticipoAnalogico = dtos.Sum(d => d.AnticipoAnalogico ?? 0);
    result.AnticipoDigitale = dtos.Sum(d => d.AnticipoDigitale ?? 0);
    result.TotaleAnticipo = result.AnticipoAnalogico + result.AnticipoDigitale;
    result.StornoAnalogico = dtos.Sum(d => d.StornoAnalogico ?? 0);
    result.StornoDigitale = dtos.Sum(d => d.StornoDigitale ?? 0);
    result.TotaleStorno = result.StornoAnalogico + result.StornoDigitale;
    result.AccontoAnalogico = dtos.Sum(d => d.AccontoAnalogico ?? 0);
    result.AccontoDigitale = dtos.Sum(d => d.AccontoDigitale ?? 0);
    result.TotaleAcconto = result.AccontoAnalogico + result.AccontoDigitale;
    result.Imponibile = dtos.Sum(d => d.TotaleFatturaSospesaImponibile ?? 0);

    var totalAmount = dtos.Sum(d => d.TotaleFatturaSospesaImponibile ?? 0);
    var totalSuspended = dtos.Sum(d => d.RelTotaleSospeso ?? 0);

    if (totalSuspended < 10)
    {
        totalSuspended = 0;
    }

    result.Totale = totalAmount - totalSuspended;

    return result;
}

    public static DocumentoContabileEmessoDettaglioResponse Map(this IEnumerable<FatturaDocContabileEmessoDettaglioDto> dto)
    {
        return new DocumentoContabileEmessoDettaglioResponse
        {
            IdTestata = dto.FirstOrDefault()?.IdFattura.ToString() ?? string.Empty,
            RagioneSociale = dto.FirstOrDefault()?.RagioneSociale,
            IdDocumento = dto.FirstOrDefault()?.IdDocumento,
            DataDocumento = dto.FirstOrDefault()?.DataFattura,
            IdEnte = dto.FirstOrDefault()?.IdEnte,
            Cup = dto.FirstOrDefault()?.Cup,
            IdContratto = dto.FirstOrDefault()?.IdContratto,
            Anno = dto.FirstOrDefault()?.Anno.ToString(),
            Mese = dto.FirstOrDefault()?.Mese.ToString(),
            TipologiaFattura = dto.FirstOrDefault()?.TipologiaFattura,
            TotaleAnalogico = dto.FirstOrDefault()?.RelTotaleAnalogico,
            TotaleDigitale = dto.FirstOrDefault()?.RelTotaleDigitale,
            TotaleNotificheAnalogiche = dto.FirstOrDefault()?.RelTotaleNotificheAnalogiche,
            TotaleNotificheDigitali = dto.FirstOrDefault()?.RelTotaleNotificheDigitali,
            Totale = dto.FirstOrDefault()?.RelTotaleNotifiche,
            TotaleFattura = dto.FirstOrDefault()?.TotaleFatturaImponibile,
            TotaleAnalogicoIva = dto.FirstOrDefault()?.RelTotaleIvatoAnalogico,
            TotaleDigitaleIva = dto.FirstOrDefault()?.RelTotaleIvatoDigitale,
            TotaleIva = dto.FirstOrDefault()?.RelTotaleIvato,
            Iva = dto.FirstOrDefault()?.Iva,
            Caricata = dto.FirstOrDefault()?.Caricata.HasValue == true && dto.FirstOrDefault()?.Caricata!.Value == true ? 1 : 0,
            TipologiaContratto = dto.FirstOrDefault()?.TipologiaContratto,
            FattureSospese = dto.Where(s => !string.IsNullOrEmpty(s.IdFatturaSospesa)).Select(s => new DocumentoContabileSospesoDettaglioResponse
            {
                IdFatturaSospesa = s.IdFatturaSospesa,
                DataFatturaSospesa = s.DataFatturaSospesa,
                Progressivo = s.ProgressivoSospesa,
                TipoDocumento = s.TipoDocumentoSospesa,
                TotaleFatturaImponibile = s.TotaleFatturaSospesaImponibile,
                TotaleFattura = s.TotaleFatturaSospesa,
                MetodoPagamento = s.MetodoPagamentoSospesa,
                CausaleFattura = s.CausaleFatturaSospesa,
                SplitPayment = s.SplitPaymentSospesa,
                Inviata = s.InviataSospesa,
                Sollecito = s.SollecitoSospesa
            }),
            AnticipoDigitale = dto.FirstOrDefault()?.AnticipoDigitale,
            AnticipoAnalogico = dto.FirstOrDefault()?.AnticipoAnalogico,
            AccontoDigitale = dto.FirstOrDefault()?.AccontoDigitale,
            AccontoAnalogico = dto.FirstOrDefault()?.AccontoAnalogico,
            StornoAnalogico = dto.FirstOrDefault()?.StornoAnalogico,
            StornoDigitale = dto.FirstOrDefault()?.StornoDigitale
        };
    }

    public static IEnumerable<DocContabileExcel> MapExport(this DocContabileBaseResponse model)
    {
        var result = new List<DocContabileExcel>();
        foreach (var item in model.Dettagli!)
        {
            foreach (var pos in item.Fattura!.Posizioni!)
            {
                result.Add(new DocContabileExcel()
                {
                    Numero = pos.NumeroLinea,
                    Posizione = pos.CodiceMateriale,
                    Totale = pos.Imponibile.ToString("0.00"),
                    PeriodoRiferimento = pos.PeriodoRiferimento
                });
            }
            result.Add(new DocContabileExcel()
            {
                Causale = item.Fattura!.CausaleFattura,
                DataFattura = item.Fattura!.DataFattura,
                Divisa = item.Fattura!.Divisa,
                IdContratto = item.Fattura!.IdContratto,
                Numero = item.Fattura!.Progressivo,
                IstitutioID = item.Fattura.IstitutioId,
                MetodoPagamento = item.Fattura.MetodoPagamento,
                OnboardingTokenID = item.Fattura.OnboardingTokenId,
                Prodotto = item.Fattura.Prodotto,
                RagioneSociale = item.Fattura.RagioneSociale,
                TipologiaFattura = item.Fattura.DatiGeneraliDocumento!.FirstOrDefault()?.TipologiaFattura,
                TipoContratto = item.Fattura.TipoContratto,
                Totale = item.Fattura.Totale!.Value.ToString("0.00") ?? null,
                Identificativo = item.Fattura.PeriodoFatturazione,
                Sollecito = item.Fattura.Sollecito,
                Split = item.Fattura.SplitPayment,
                TipoDocumento = item.Fattura.TipoDocumento,
                Posizione = "totale:",
            });

            result.Add(new DocContabileExcel());
        }
        return result;
    }

    public static FattureQueryRicercaByEnte Map(this FatturaRicercaEnteRequest req, AuthenticationInfo authInfo)
    {
        return new FattureQueryRicercaByEnte(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            TipologiaFattura = req.TipologiaFattura
        };
    }

    public static FatturaRiptristinoSAPCommandList Map2(this List<FatturaPipelineSapRequest> request, AuthenticationInfo authInfo, bool invio)
    {
        return new FatturaRiptristinoSAPCommandList(
            request.Select(x => new FatturaRiptristinoSAPCommand(authInfo, x.AnnoRiferimento!.Value, x.MeseRiferimento!.Value, x.TipologiaFattura)
            {
                Invio = invio
            })
        .ToList());
    }

    public static FatturaInvioSap Map(this FatturaPipelineSapRequest request)
    {
        return new FatturaInvioSap()
        {
            AnnoRiferimento = request.AnnoRiferimento!.Value,
            MeseRiferimento = request.MeseRiferimento!.Value,
            TipologiaFattura = request.TipologiaFattura
        };
    }

    public static List<FatturaInvioSap> Map(this List<FatturaPipelineSapRequest> request)
    {
        return request.Select(x => new FatturaInvioSap()
        {
            AnnoRiferimento = x.AnnoRiferimento!.Value,
            MeseRiferimento = x.MeseRiferimento!.Value,
            TipologiaFattura = x.TipologiaFattura
        }).ToList();
    }

    public static Dictionary<string, object> ToDictionary<T>(this T obj)
    {
        var dict = new Dictionary<string, object>();
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }
        var type = typeof(T);
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = property.GetValue(obj);
            dict[property.Name] = value!;
        }
        return dict;
    }

    public static async Task<Dictionary<string, byte[]>> ReportFatture(this FatturaRicercaRequest request, IMediator handler, AuthenticationInfo authInfo)
    {
        Dictionary<string, byte[]> reports = [];

        if (request.TipologiaFattura!.IsNullNotAny())
        {
            request.TipologiaFattura = (await handler.Send(new FattureTipologiaAnniMeseQuery(authInfo)
            {
                Anno = request.Anno!,
                Mese = request.Mese!
            }))!.ToArray();
        }

        foreach (var tipologia in request.TipologiaFattura!)
        {
            var month = request.Mese.GetMonth();
            var year = request.Anno;

            switch (tipologia)
            {
                case TipologiaFattura.PRIMOSALDO:
                    var fatture = await handler.Send(request.Mapv2(authInfo, tipologia));
                    if (fatture.IsNotEmpty())
                        reports.Add($"Lista {tipologia} {year} {month}", fatture!.ReportFattureRel(month, tipologia));
                    break;
                case TipologiaFattura.SECONDOSALDO:
                    fatture = await handler.Send(request.Mapv2(authInfo, tipologia));
                    if (fatture.IsNotEmpty())
                        reports.Add($"Lista {tipologia} {year} {month}", fatture!.ReportFattureRel(request.Mese.GetMonth(), tipologia));
                    break;
                case TipologiaFattura.VAR_SEMESTRALE:
                    fatture = await handler.Send(request.Mapv2(authInfo, tipologia));
                    if (fatture.IsNotEmpty())
                        reports.Add($"Lista {tipologia} {year} {month}", fatture!.ReportFattureRel(request.Mese.GetMonth(), tipologia));
                    break;
                case TipologiaFattura.ANTICIPO:
                    var commesse = await handler.Send(request.Mapv3(authInfo));
                    if (commesse.IsNotEmpty())
                        reports.Add($"Lista ANTICIPO {year} {month}", commesse!.ReportFattureModuloCommessa(request.Mese.GetMonth()));
                    break;
                case TipologiaFattura.ACCONTO:
                    var acconto = await handler.Send(request.Mapv4(authInfo));
                    if (acconto.IsNotEmpty())
                        reports.Add($"Lista ACCONTO {year} {month}", acconto!.ReportFattureAcconto(request.Mese.GetMonth()));
                    break;
                default:
                    break;
            }
        }
        return reports;
    }

    public static bool IsNotEmpty<T>(this List<IEnumerable<T>>? model)
    {
        if (model == null)
            return false;
        if (model.Count == 0)
            return false;
        var count = 0;
        foreach (var item in model)
            count += item.Count();

        if (count == 0)
            return false;
        return true;
    }

    public static byte[] ReportFattureModuloCommessa(this List<IEnumerable<FattureCommessaExcelDto>> commesse, string month)
    {
        DataSet? dataSet = new();
        for (var i = 0; i < commesse.Count; i++)
        {
            if (i == 0)
                dataSet.Tables.Add(commesse[i]!.FillTableWithTotalsRel(0, $"Com. {month}"));
            else if (i == 1)
                dataSet.Tables.Add(commesse[i]!.FillTableWithTotalsRel(0, $"Com. stimate {month}"));
            else if (i == 2)
                dataSet.Tables.Add(commesse[i]!.FillTableWithTotalsRel(0, $"Com. stimate fatt. {month}"));
            else if (i == 3)
                dataSet.Tables.Add(commesse[i]!.FillTableWithTotalsRel(0, $"Com. fatt. {month}"));
            else if (i == 4)
                dataSet.Tables.Add(commesse[i]!.FillTableWithTotalsRel(0, $"Com. non fatt. {month}"));
            else
                dataSet.Tables.Add(commesse[i]!.FillTableWithTotalsRel(0, $"Com. eliminate fatt. {month}"));
        }

        using var memory = dataSet!.ToExcel();
        return memory.ToArray();
    }

    public static byte[] ReportFattureAcconto(this List<IEnumerable<FattureAccontoExcelDto>> commesse, string month)
    {
        DataSet? dataSet = new();
        for (var i = 0; i < commesse.Count; i++)
        {
            var fattureNONzero = commesse[i].Where(x => x.TotaleFattura != 0);
            if (!fattureNONzero.IsNullNotAny())
                dataSet.Tables.Add(fattureNONzero!.FillTableWithTotalsRel(0, $"Acconto {month}"));
            var fatturezero = commesse[i].Where(x => x.TotaleFattura == 0);
            if (!fatturezero.IsNullNotAny())
                dataSet.Tables.Add(fatturezero!.FillTableWithTotalsRel(0, $"Acconto a zero {month}"));
        }
        using var memory = dataSet!.ToExcel();
        return memory.ToArray();
    }

    public static byte[] ReportFattureRel(this List<IEnumerable<FattureRelExcelDto>> fatture, string month, string tipologia)
    {
        DataSet? dataSet = new();
        for (var i = 0; i < fatture.Count; i++)
        {
            if (i == 0)
                dataSet.Tables.Add(fatture[i]!.FillTableWithTotalsRel(9, $"Regolari Esecuzioni {month}"));
            else if (i == 1)
            {
                var fattureNONzero = fatture[i].Where(x => x.TotaleFatturaImponibile != 0);
                dataSet.Tables.Add(fattureNONzero!.FillTableWithTotalsRel(9, $"Enti Fatt. {month}"));
                var fatturezero = fatture[i].Where(x => x.TotaleFatturaImponibile == 0);
                if (!fatturezero.IsNullNotAny())
                    dataSet.Tables.Add(fatturezero!.FillTableWithTotalsRel(9, $"Enti Fatt. a Zero {month}"));
            }
            else
                dataSet.Tables.Add(fatture[i]!.FillTableWithTotalsRel(9, $"Note di Credito {month}"));
        }

        using var memory = dataSet!.ToExcel();
        return memory.ToArray();
    }

    public static (MessaggioCreateCommand, DocumentiStorageKey) Mapv2(this FatturaRicercaRequest req, AuthenticationInfo authInfo, string? contentType, string? contentLanguage)
    {
        var command = new MessaggioCreateCommand(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            Json = req.Serialize(),
            TipologiaDocumento = TipologiaDocumento.Fatturazione,
            ContentType = contentType,
            ContentLanguage = contentLanguage
        };

        var key = new DocumentiStorageKey(authInfo.IdEnte, authInfo.Id, TipologiaDocumento.Fatturazione, command.DataInserimento.Year, command.Hash);
        command.LinkDocumento = key.ToString();
        return (command, key);
    }

    public static FattureQueryRicerca Map(this FatturaRicercaRequest req, AuthenticationInfo authInfo)
    {
        return new FattureQueryRicerca(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = req.IdEnti,
            TipologiaFattura = req.TipologiaFattura,
            Cancellata = req.Cancellata == null ? false : req.Cancellata.Value,
            FkIdTipoContratto = req.IdTipoContratto,
            FatturaInviata = req.Inviata
        };
    }

    public static FattureRelExcelQuery Mapv2(this FatturaRicercaRequest req, AuthenticationInfo authInfo, string tipologiaFattura)
    {
        return new FattureRelExcelQuery(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = req.IdEnti,
            TipologiaFattura = tipologiaFattura,
            FkIdTipoContratto = req.IdTipoContratto,
            FatturaInviata = req.Inviata
        };
    }

    public static FattureCommessaExcelQuery Mapv3(this FatturaRicercaRequest req, AuthenticationInfo authInfo)
    {
        return new FattureCommessaExcelQuery(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = req.IdEnti,
            FkIdTipoContratto = req.IdTipoContratto,
            FatturaInviata = req.Inviata
        };
    }

    public static FattureAccontoExcelQuery Mapv4(this FatturaRicercaRequest req, AuthenticationInfo authInfo)
    {
        return new FattureAccontoExcelQuery(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = req.IdEnti,
            FkIdTipoContratto = req.IdTipoContratto,
            FatturaInviata = req.Inviata
        };
    }

    public static IEnumerable<FattureExcel> Map(this FattureListaDto model)
    {
        var result = new List<FattureExcel>();
        foreach (var item in model)
        {
            foreach (var pos in item.fattura!.Posizioni!)
            {
                result.Add(new FattureExcel()
                {
                    Numero = item.fattura!.Numero,
                    Posizione = pos.CodiceMateriale,
                    Totale = pos.Imponibile.ToString("0.00"),
                    PeriodoRiferimento = pos.PeriodoRiferimento
                });
            }
            result.Add(new FattureExcel()
            {
                Causale = item.fattura!.Causale,
                DataFattura = item.fattura!.DataFattura,
                Divisa = item.fattura!.Divisa,
                IdContratto = item.fattura!.IdContratto,
                Numero = item.fattura!.Numero,
                IstitutioID = item.fattura.IstitutioID,
                MetodoPagamento = item.fattura.MetodoPagamento,
                OnboardingTokenID = item.fattura.OnboardingTokenID,
                Prodotto = item.fattura.Prodotto,
                RagioneSociale = item.fattura.RagioneSociale,
                TipologiaFattura = item.fattura.TipologiaFattura,
                TipoContratto = item.fattura.TipoContratto,
                Totale = item.fattura.Totale.ToString("0.00"),
                Identificativo = item.fattura.Identificativo,
                Sollecito = item.fattura.Sollecito,
                Split = item.fattura.Split,
                TipoDocumento = item.fattura.TipoDocumento,
                Posizione = "totale:",
            });

            result.Add(new FattureExcel());
        }
        return result;
    }

    public static byte[] CreateZip(this
    Dictionary<string, byte[]> reports,
    ILogger<FattureModule> logger,
    string extension = ".xlsx")
    {
        byte[] zipBytes;
        using var memoryStreamZip = reports.CreateMemoryStreamZip(logger);
        {
            zipBytes = memoryStreamZip.ToArray();
            memoryStreamZip.Flush();
        }
        return zipBytes;
    }

    public static MemoryStream CreateMemoryStreamZip(this
        Dictionary<string, byte[]> reports,
        ILogger<FattureModule> logger,
        string extension = ".xlsx")
    {

        var memoryStreamZip = new MemoryStream();
        using var zipArchive = new ZipArchive(memoryStreamZip, ZipArchiveMode.Create, leaveOpen: true);
        {
            memoryStreamZip.Position = 0;
            foreach (var report in reports!)
            {
                try
                {
                    zipArchive.AddFile(report.Key + extension, report.Value);
                }
                catch
                {
                    var msg = $"Errore nel legger il file {report.Key}!";
                    logger.LogError(msg);
                }
            }
            zipArchive.Dispose();
        }
        return memoryStreamZip;
    }
}
