using System.Globalization;
using System.Text.RegularExpressions;
using PortaleFatture.BE.Api.Modules.Tipologie.Payload.Payload.Response;
using PortaleFatture.BE.Core.Entities.Scadenziari;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni.Extensions;
public static class TipologieExtensions
{
    private static CultureInfo culture = new("it-IT");

    public static IEnumerable<CalendarioContestazioniResponse> Mapper(this IEnumerable<CalendarioContestazione> models)
    {
        return models.Select(x =>
           new CalendarioContestazioniResponse()
           {
               AnnoContestazione = x.AnnoContestazione,
               DataFine = x.DataFine.ToString("dd MMMM yyyy",culture),
               DataInizio = x.DataInizio.ToString("dd MMMM yyyy", culture),
               MeseContestazione = x.MeseContestazione.GetMonth(),
               DataRecapitistaFine = x.DataVerifica.ToString("dd MMMM yyyy", culture),
               DataRecapitistaInizio = x.DataFine.AddDays(1).ToString("dd MMMM yyyy", culture)
           }
        );
    }
}