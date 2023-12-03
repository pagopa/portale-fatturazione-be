using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Core.Extensions;

public static class DocumentExtensions
{
    private static string _tableDetails = @"
            <tr class='{0}'>
                <td>[Tipo]</td>
                <td>[NumeroNotificheNazionali]</td>
                <td>[NumeroNotificheInternazionali]</td>
                <td>[TotaleNotifiche]</td>
            </tr> 
        ";

    public static string GetModuloCommessa(this IEnumerable<DatiModuloCommessaTotaleDto>? commesse)
    {
        StringBuilder builder = new();
        for (var i = 0; i < commesse!.Count(); i++)
        {
            var commessa = commesse!.ToList()[i];
            var table = String.Format(_tableDetails, i == commesse!.Count() - 1 ? "ending" : "details")
              .Replace(nameof(commessa.Tipo).GetName<DatiModuloCommessaTotaleDto>(), commessa.Tipo)
              .Replace(nameof(commessa.NumeroNotificheNazionali).GetName<DatiModuloCommessaTotaleDto>(), commessa.NumeroNotificheNazionali.ToString())
              .Replace(nameof(commessa.NumeroNotificheInternazionali).GetName<DatiModuloCommessaTotaleDto>(), commessa.NumeroNotificheInternazionali.ToString())
              .Replace(nameof(commessa.TotaleNotifiche).GetName<DatiModuloCommessaTotaleDto>(), commessa.TotaleNotifiche.ToString())
              ;
            builder.Append(table);
        }
        return builder.ToString();
    }

    public static string GetContatti(this IEnumerable<DatiFatturazioneContatto>? contatti)
    {
        StringBuilder builder = new();
        foreach (var contatto in contatti!)
        {
            builder.Append(contatto.Email.GetEmail() + "<br/>");
        }
        return builder.ToString();
    }

    public static string GetMonth(this int month)
    {
        return new DateTime(1999, month, 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("it-IT")).ToUpperInvariant();
    }

    public static string GetEmail(this string? email)
    {
        return $"<a href = \"mailto:{email}\" >{email}</a>";
    }

    public static string GetData(this DateTimeOffset? data)
    {
        var d = data!.Value;
        return new DateTime(d.Year, d.Month, d.Day).ToString("d", new CultureInfo("it-IT"));
    }

    private static Dictionary<string, string> _names = new();
    private static string GetName<T>(this string propertyName)
    {
        _names.TryGetValue(propertyName, out var name); 
        if(name == null)
        {
            var propertyInfo = typeof(T)!.GetProperty(propertyName)!;
            name =  $"[{propertyInfo.Name!}]";
            _names.TryAdd(propertyName, name); 
        }
        return name;
    } 

    public static string Replace(this ModuloCommessaDocumentoDto model, string template)
    {

        template = template
            .Replace(nameof(model.Descrizione).GetName<ModuloCommessaDocumentoDto>(), model.Descrizione)
            .Replace(nameof(model.PartitaIva).GetName<ModuloCommessaDocumentoDto>(), model.PartitaIva)
            .Replace(nameof(model.IndirizzoCompleto).GetName<ModuloCommessaDocumentoDto>(), model.IndirizzoCompleto)

            .Replace(nameof(model.Cup).GetName<ModuloCommessaDocumentoDto>(), model.Cup)
            .Replace(nameof(model.Cig).GetName<ModuloCommessaDocumentoDto>(), model.Cig)
            .Replace(nameof(model.Descrizione).GetName<ModuloCommessaDocumentoDto>(), model.Cig)
            .Replace(nameof(model.CodCommessa).GetName<ModuloCommessaDocumentoDto>(), model.CodCommessa)
            .Replace(nameof(model.SplitPayment).GetName<ModuloCommessaDocumentoDto>(), model.SplitPayment)
            .Replace(nameof(model.Map).GetName<ModuloCommessaDocumentoDto>(), model.Map)
            .Replace(nameof(model.TipoCommessa).GetName<ModuloCommessaDocumentoDto>(), model.TipoCommessa)
            .Replace(nameof(model.Prodotto).GetName<ModuloCommessaDocumentoDto>(), model.Prodotto)
            .Replace(nameof(model.Pec).GetName<ModuloCommessaDocumentoDto>(), model.Pec.GetEmail())
            .Replace(nameof(model.Contatti).GetName<ModuloCommessaDocumentoDto>(), model.Contatti.GetContatti())
            .Replace(nameof(model.DataModifica).GetName<ModuloCommessaDocumentoDto>(), model.DataModifica.GetData())
            .Replace(nameof(model.MeseAttivita).GetName<ModuloCommessaDocumentoDto>(), model.MeseAttivita.GetMonth())

            .Replace(nameof(model.DatiModuloCommessa).GetName<ModuloCommessaDocumentoDto>(), model.DatiModuloCommessa.GetModuloCommessa())
            ;

        return template;
    }

    public static ModuloCommessaDocumentoDto Mapper(this ModuloCommessaAggregateDto model)
    {
        var fatt = model.DatiFatturazione!;
        var ente = model.Ente!;
        var modulo = model.DatiModuloCommessa!;
        var categorie = model.Categorie;
        var tipi = categorie!.SelectMany(x => x.TipoSpedizione!);

        var moduloDocumento = new ModuloCommessaDocumentoDto()
        {
            Descrizione = ente.Descrizione,
            PartitaIva = ente.PartitaIva,
            IndirizzoCompleto = ente.IndirizzoCompleto,

            Cup = fatt.Cup,
            Cig = fatt.Cig,
            CodCommessa = fatt.CodCommessa,
            Contatti = fatt.Contatti,
            DataDocumento = fatt.DataDocumento,
            DataModifica = fatt.DataModifica,
            SplitPayment = fatt.SplitPayment == true ? "Attivo" : string.Empty,
            IdDocumento = fatt.IdDocumento,
            Map = fatt.Map,
            TipoCommessa = fatt.TipoCommessa == "1" ? "Ordine" : "Contratto",
            Pec = fatt.Pec,
            Prodotto = fatt.Prodotto,
            MeseAttivita = modulo.Select(x => x.MeseValidita).FirstOrDefault(),
            DatiModuloCommessa = modulo.Select(x => new DatiModuloCommessaTotaleDto()
            {
                Tipo = tipi!.Where(y => y.Id == x.IdTipoSpedizione).FirstOrDefault()!.Descrizione,
                NumeroNotificheInternazionali = x.NumeroNotificheInternazionali,
                NumeroNotificheNazionali = x.NumeroNotificheNazionali,
                TotaleNotifiche = x.NumeroNotificheInternazionali + x.NumeroNotificheNazionali,
                IdTipoSpedizione = x.IdTipoSpedizione
            })
        };

        var list = moduloDocumento.DatiModuloCommessa.ToList();
        list.Add(
           new DatiModuloCommessaTotaleDto()
           {
               NumeroNotificheInternazionali = moduloDocumento.DatiModuloCommessa.Select(x => x.NumeroNotificheInternazionali).Sum(),
               NumeroNotificheNazionali = moduloDocumento.DatiModuloCommessa.Select(x => x.NumeroNotificheNazionali).Sum(),
               TotaleNotifiche = moduloDocumento.DatiModuloCommessa.Select(x => x.TotaleNotifiche).Sum(),
               Tipo = "Totale notifiche da processare"
           });
        moduloDocumento.DatiModuloCommessa = list;
        return moduloDocumento;
    }
}
