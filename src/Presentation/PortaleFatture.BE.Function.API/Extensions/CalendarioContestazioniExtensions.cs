using System.Globalization;
using PortaleFatture.BE.Api.Modules.SEND.Tipologie.Payload.Payload.Response;
using PortaleFatture.BE.Core.Entities.SEND.Scadenziari;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;

namespace PortaleFatture.BE.Function.API.Extensions;

public static class CalendarioContestazioniExtensions
{
    private static readonly StatoFlag[] _defaultStatiFlag =
    [
        new StatoFlag { Id = 2, Flag = "Annullata", Descrizione = "Anullata" },
        new StatoFlag { Id = 3, Flag = "Contestata Ente", Descrizione = "Contestazione" },
        new StatoFlag { Id = 7, Flag = "Risposta Ente", Descrizione = "Contestazione" },
        new StatoFlag { Id = 9, Flag = "Rifiutata/Chiusa", Descrizione = "Risoluzione" }
    ];

    /// <summary>
    /// Converte un CalendarioContestazioniStatoResponse in versione estesa con flag consentiti
    /// </summary>
    public static CalendarioContestazioniExtendedResponse ToExtendedResponse(this CalendarioContestazioniStatoResponse source)
    {
        var extended = new CalendarioContestazioniExtendedResponse
        {
            DataFine = source.DataFine,
            DataInizio = source.DataInizio,
            ChiusuraContestazioni = source.ChiusuraContestazioni,
            TempoRisposta = source.TempoRisposta,
            MeseContestazione = source.MeseContestazione,
            DescrizioneMeseContestazione = source.DescrizioneMeseContestazione,
            AnnoContestazione = source.AnnoContestazione,
            StatiFlag = _defaultStatiFlag,
            FlagConsentiti = new FlagConsentiti
            {
                PeriodoContestazione = new PeriodoFlag
                {
                    Periodo = "DataInizio - DataFine",
                    Date = $"{source.DataInizio} - {source.DataFine}",
                    FlagStatoPermessi = [2, 3, 7, 9],
                    Descrizione = "Tutte le azioni consentite"
                },
                PeriodoRisposta = new PeriodoFlag
                {
                    Periodo = "DataFine - ChiusuraContestazioni",
                    Date = $"{source.DataFine?.ToString("yyyy-MM-dd")} - {source.ChiusuraContestazioni}",
                    FlagStatoPermessi = [7, 9],
                    Descrizione = "Solo risposte e accettazioni"
                },
                PeriodoChiuso = new PeriodoFlag
                {
                    Periodo = "Dopo ChiusuraContestazioni",
                    Date = $"Dopo {source.ChiusuraContestazioni}",
                    FlagStatoPermessi = [],
                    Descrizione = "Nessuna azione consentita"
                }
            }
        };
        return extended;
    }

    /// <summary>
    /// Converte un IEnumerable di CalendarioContestazioniStatoResponse in versioni estese
    /// </summary>
    public static IEnumerable<CalendarioContestazioniExtendedResponse> ToExtendedResponses(this IEnumerable<CalendarioContestazioniStatoResponse> source)
    {
        return source.Select(x => x.ToExtendedResponse());
    }

    /// <summary>
    /// Restituisce i flag consentiti per una data specifica
    /// </summary>
    public static int[] GetFlagConsentiti(this CalendarioContestazioniStatoResponse source, DateTime dataRiferimento)
    {
        // Periodo principale: tutte le azioni consentite
        if (dataRiferimento >= source.DataInizio && dataRiferimento <= source.DataFine)
        {
            return [2, 3, 7, 9];
        }

        // Periodo di grazia: solo risposte e accettazioni
        if (dataRiferimento > source.DataFine && dataRiferimento <= source.ChiusuraContestazioni)
        {
            return [7, 8];
        }

        // Periodo chiuso: nessuna azione consentita
        return [];
    }

    /// <summary>
    /// Verifica se un flag è consentito per una data specifica
    /// </summary>
    public static bool IsFlagConsentito(this CalendarioContestazioniStatoResponse source, int flagId, DateTime dataRiferimento)
    {
        var flagConsentiti = source.GetFlagConsentiti(dataRiferimento);
        return flagConsentiti.Contains(flagId);
    }

    /// <summary>
    /// Verifica se un flag è consentito oggi
    /// </summary>
    public static bool IsFlagConsentitoOggi(this CalendarioContestazioniStatoResponse source, int flagId)
    {
        return source.IsFlagConsentito(flagId, DateTime.Today);
    }

    private static CultureInfo culture = new("it-IT");

    public static IEnumerable<CalendarioContestazioniStatoResponse> Mapperv2(this IEnumerable<CalendarioContestazione> models)
    {
        return models.Select(x =>
           new CalendarioContestazioniStatoResponse()
           {
               AnnoContestazione = x.AnnoContestazione,
               DataFine = x.DataFine,
               DataInizio = x.DataInizio,
               ChiusuraContestazioni = x.ChiusuraContestazioni,
               TempoRisposta = x.TempoRisposta,
               DescrizioneMeseContestazione = x.MeseContestazione.GetMonth(),
               MeseContestazione = x.MeseContestazione,
               DataRecapitistaFine = x.DataVerifica,
               DataRecapitistaInizio = x.DataFine.AddDays(1)
           }
        );
    }
}