using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using PortaleFatture.BE.Core.Entities.SEND.DatiFatturazioni;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;

namespace PortaleFatture.BE.Core.Extensions;

public static class DocumentExtensions
{
    public static string GetHashSHA256(this string? rawData)
    { 
        using var sha256Hash = SHA256.Create();
        var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData!)); 
        var builder = new StringBuilder();
        for (var i = 0; i < bytes.Length; i++) 
            builder.Append(bytes[i].ToString("x2")); 
        return builder.ToString();
    }

    public static string GetHashSHA512(this byte[] document)
    {
        using var sha512Hash = SHA512.Create();
        var hashBytes = sha512Hash.ComputeHash(document);
        return BitConverter.ToString(hashBytes).Replace("-", String.Empty);
    }

    public static string MapRelTestata(this byte caricata)
    {
        if (caricata == 0)
            return "Non Caricata";
        else if (caricata == 1)
            return "Firmata";
        else if (caricata == 2)
            return "Invalidata";
        else
            return "Non Caricata";
    }

    private static string _tableModuloCommessa = @"
            <tr class='{0}'>
                <td>[Tipo]</td>
                <td>[NumeroNotificheNazionali]</td>
                <td>[NumeroNotificheInternazionali]</td>
                <td>[TotaleNotifiche]</td>
            </tr> 
        ";

    private static string _tableTotali = @"
            <tr class='{0}'>
                <td colspan='2'>[Descrizione]</td>
                <td colspan='2'>[Totale] €</td> 
            </tr> 
        ";

    public static string GetModuloCommessa(this IEnumerable<DatiModuloCommessaTotaleDto>? commesse)
    {
        StringBuilder builder = new();
        List<DatiModuloCommessaTotaleDto> ordineCommessa = [];
        ordineCommessa.Add(commesse!.Where(x => x.IdTipoSpedizione == 3).FirstOrDefault()!);
        ordineCommessa.Add(commesse!.Where(x => x.IdTipoSpedizione == 1).FirstOrDefault()!);
        ordineCommessa.Add(commesse!.Where(x => x.IdTipoSpedizione == 2).FirstOrDefault()!);
        ordineCommessa.Add(commesse!.Where(x => x.IdTipoSpedizione == 0).FirstOrDefault()!);

        for (var i = 0; i < ordineCommessa!.Count; i++)
        {
            var commessa = ordineCommessa!.ToList()[i];
            if(commessa!=null)
            {
                var table = String.Format(_tableModuloCommessa, i == commesse!.Count() - 1 ? "ending" : "details")
                  .Replace(nameof(commessa.Tipo).GetName<DatiModuloCommessaTotaleDto>(), commessa.Tipo)
                  .Replace(nameof(commessa.NumeroNotificheNazionali).GetName<DatiModuloCommessaTotaleDto>(), commessa.NumeroNotificheNazionali.ToString())
                  .Replace(nameof(commessa.NumeroNotificheInternazionali).GetName<DatiModuloCommessaTotaleDto>(), commessa.NumeroNotificheInternazionali.ToString())
                  .Replace(nameof(commessa.TotaleNotifiche).GetName<DatiModuloCommessaTotaleDto>(), commessa.TotaleNotifiche.ToString())
                  ;
                builder.Append(table);
            } 
        }
        return builder.ToString();
    }

    public static string GetModuloCommessaTotali(this IEnumerable<DatiModuloCommessaTotaleCostoDto> totali)
    {
        StringBuilder builder = new();

        for (var i = 0; i < totali!.Count(); i++)
        {
            var totale = totali!.ToList()[i];
            var table = String.Format(_tableTotali, i == totali!.Count() - 1 ? "ending" : "details")
            .Replace(nameof(totale.Descrizione).GetName<DatiModuloCommessaTotaleCostoDto>(), totale.Descrizione)
            .Replace(nameof(totale.Totale).GetName<DatiModuloCommessaTotaleCostoDto>(), totale.Totale.ToString())
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

    public static string GetMonth(this int? month)
    {
        return GetMonth(month!.Value);
    }

    public static string GetEmail(this string? email)
    {
        return $"<a href = \"mailto:{email}\" >{email}</a>";
    }

    public static string GetData(this DateTimeOffset? data)
    {
        var d = data!.Value;
        return new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second).ToString(new CultureInfo("it-IT"));
    }

    private static Dictionary<string, string> _names = new();
    public static string GetName<T>(this string propertyName)
    {
        _names.TryGetValue(propertyName, out var name);
        if (name == null)
        {
            var propertyInfo = typeof(T)!.GetProperty(propertyName)!;
            name = $"[{propertyInfo.Name!}]";
            _names.TryAdd(propertyName, name);
        }
        return name;
    }

    public static string Replace(this RelDocumentoDto model, string template)
    {

        template = template
            .Replace(nameof(model.Anno).GetName<RelDocumentoDto>(), model.Anno)
            .Replace(nameof(model.Mese).GetName<RelDocumentoDto>(), model.Mese)
            .Replace(nameof(model.Totale).GetName<RelDocumentoDto>(), model.Totale!.ToString().Replace(".", "."))
            .Replace(nameof(model.TotaleAnalogico).GetName<RelDocumentoDto>(), model.TotaleAnalogico!.ToString().Replace(".", "."))
            .Replace(nameof(model.TotaleDigitale).GetName<RelDocumentoDto>(), model.TotaleDigitale!.ToString().Replace(".", "."))
            .Replace(nameof(model.TotaleNotificheAnalogiche).GetName<RelDocumentoDto>(), model.TotaleNotificheAnalogiche!.ToString())
            .Replace(nameof(model.TotaleNotificheDigitali).GetName<RelDocumentoDto>(), model.TotaleNotificheDigitali!.ToString())
            .Replace(nameof(model.TipologiaFattura).GetName<RelDocumentoDto>(), model.TipologiaFattura!)
            .Replace(nameof(model.RagioneSociale).GetName<RelDocumentoDto>(), model.RagioneSociale!)
            .Replace(nameof(model.IdContratto).GetName<RelDocumentoDto>(), model.IdContratto!)
              ;

        return template;
    }

    public static string Replace(this RelEmail model, string template)
    {

        template = template
            .Replace(nameof(model.Anno).GetName<RelEmail>(), model.Anno.ToString())
            .Replace(nameof(model.Mese).GetName<RelEmail>(), model.Mese.GetMonth())
            .Replace(nameof(model.IdEnte).GetName<RelEmail>(), model.IdEnte) 
            .Replace(nameof(model.IdContratto).GetName<RelEmail>(), model.IdContratto)
            .Replace(nameof(model.TipologiaFattura).GetName<RelEmail>(), model.TipologiaFattura)
            .Replace(nameof(model.Pec).GetName<RelEmail>(), model.Pec)
            .Replace(nameof(model.RagioneSociale).GetName<RelEmail>(), model.RagioneSociale)
            ;

        return template;
    }

    public static string Replace(this ModuloCommessaDocumentoDto model, string template)
    {

        template = template
            .Replace(nameof(model.Descrizione).GetName<ModuloCommessaDocumentoDto>(), model.Descrizione)
            .Replace(nameof(model.PartitaIva).GetName<ModuloCommessaDocumentoDto>(), model.PartitaIva)
            .Replace(nameof(model.IndirizzoCompleto).GetName<ModuloCommessaDocumentoDto>(), model.IndirizzoCompleto)

            .Replace(nameof(model.Cup).GetName<ModuloCommessaDocumentoDto>(), model.Cup)
            .Replace(nameof(model.Descrizione).GetName<ModuloCommessaDocumentoDto>(), model.Descrizione)
            .Replace(nameof(model.CodCommessa).GetName<ModuloCommessaDocumentoDto>(), model.CodCommessa)
            .Replace(nameof(model.SplitPayment).GetName<ModuloCommessaDocumentoDto>(), model.SplitPayment)
            .Replace(nameof(model.Map).GetName<ModuloCommessaDocumentoDto>(), model.Map)
            .Replace(nameof(model.TipoCommessa).GetName<ModuloCommessaDocumentoDto>(), model.TipoCommessa)
            .Replace(nameof(model.Prodotto).GetName<ModuloCommessaDocumentoDto>(), model.Prodotto)
            .Replace(nameof(model.Pec).GetName<ModuloCommessaDocumentoDto>(), model.Pec.GetEmail())
            .Replace(nameof(model.Contatti).GetName<ModuloCommessaDocumentoDto>(), model.Contatti.GetContatti())
            .Replace(nameof(model.DataModifica).GetName<ModuloCommessaDocumentoDto>(), model.DataModifica.GetData())
            .Replace(nameof(model.MeseAttivita).GetName<ModuloCommessaDocumentoDto>(), model.MeseAttivita)

            .Replace(nameof(model.DatiModuloCommessa).GetName<ModuloCommessaDocumentoDto>(), model.DatiModuloCommessa.GetModuloCommessa())

            .Replace(nameof(model.DatiModuloCommessaCosti).GetName<ModuloCommessaDocumentoDto>(), model.DatiModuloCommessaCosti!.GetModuloCommessaTotali())
            ;

        return template;
    }

    public static ModuloCommessaDocumentoDto Mapper(this ModuloCommessaAggregateDto model)
    {
        var fatt = model.DatiFatturazione!;
        var ente = model.Ente!;
        var modulo = model.DatiModuloCommessa!;
        var tipoContratto = modulo.Select(x => x.IdTipoContratto).FirstOrDefault();
        var confModulo = model.DatiConfigurazioneModuloCommessa;
        var totaleModulo = model.DatiModuloCommessaTotale!;

        var tipi = confModulo!.Tipi;
        var mese = model.DatiModuloCommessa!.Select(x => x.MeseValidita).FirstOrDefault().GetMonth();
        var anno = model.DatiModuloCommessa!.Select(x => x.AnnoValidita).FirstOrDefault();
        var data = $"{mese}/{anno}";
        var dataModificaCommessa = modulo.Select(x => x.DataModifica).FirstOrDefault();
        var dataCreazioneCommessa = modulo.Select(x => x.DataCreazione).FirstOrDefault();

        var moduloDocumento = new ModuloCommessaDocumentoDto()
        {
            Descrizione = ente.Descrizione,
            PartitaIva = ente.PartitaIva,
            IndirizzoCompleto = ente.IndirizzoCompleto,

            Cup = fatt.Cup,
            CodCommessa = fatt.CodCommessa,
            Contatti = fatt.Contatti,
            DataDocumento = fatt.DataDocumento,
            DataModifica = dataModificaCommessa == DateTime.MinValue ? dataCreazioneCommessa : dataModificaCommessa,
            SplitPayment = fatt.SplitPayment == null? null : fatt.SplitPayment == true ? "SI" : "NO",
            IdDocumento = fatt.IdDocumento,
            Map = fatt.Map,
            TipoCommessa = fatt.TipoCommessa == "1" ? "Ordine" : "Contratto",
            Pec = fatt.Pec,
            Prodotto = fatt.Prodotto == "prod-pn" ? "SEND" : fatt.Prodotto,
            MeseAttivita = data,
            DatiModuloCommessa = modulo.Select(x => new DatiModuloCommessaTotaleDto()
            {
                Tipo = tipi!.Where(y => y.IdTipoSpedizione == x.IdTipoSpedizione).FirstOrDefault()!.Descrizione!.Replace("[data]", data),
                NumeroNotificheInternazionali = x.NumeroNotificheInternazionali,
                NumeroNotificheNazionali = x.NumeroNotificheNazionali,
                TotaleNotifiche = x.NumeroNotificheInternazionali + x.NumeroNotificheNazionali,
                IdTipoSpedizione = x.IdTipoSpedizione
            }),
            DatiModuloCommessaCosti = []
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

        var catDigitale = confModulo.Categorie!.Where(x => x.IdCategoriaSpedizione == 2 && x.IdTipoContratto == tipoContratto);
        var catAnalogico = confModulo.Categorie!.Where(x => x.IdCategoriaSpedizione == 1 && x.IdTipoContratto == tipoContratto);

        var percentDigitale = catDigitale.Select(x => x.Percentuale).FirstOrDefault()!;
        var percentAnalogico = catAnalogico.Select(x => x.Percentuale).FirstOrDefault()!;

        var digitale = catDigitale.Select(x => x.Descrizione).FirstOrDefault()!.Replace("[data]", data).Replace("[percent]", percentDigitale.ToString());
        var analogico = catAnalogico.Select(x => x.Descrizione).FirstOrDefault()!.Replace("[data]", data).Replace("[percent]", percentAnalogico.ToString());

        totaleModulo.Totali!.TryGetValue(2, out var totaleDigitale);
        totaleModulo.Totali!.TryGetValue(1, out var totaleAnalogico);

        var totali = moduloDocumento.DatiModuloCommessaCosti!.ToList();
        totali.Add(
           new DatiModuloCommessaTotaleCostoDto()
           {
               Totale = totaleDigitale == null ? 0 : totaleDigitale.TotaleCategoria,
               Descrizione = digitale
           });
        totali.Add(
           new DatiModuloCommessaTotaleCostoDto()
           {
               Totale = totaleAnalogico == null ? 0 : totaleAnalogico.TotaleCategoria,
               Descrizione = analogico
           });
        totali.Add(
           new DatiModuloCommessaTotaleCostoDto()
           {
               Totale = totaleModulo!.Totale,
               Descrizione = "TOTALE MODULO COMMESSA NETTO IVA"
           });
        moduloDocumento.DatiModuloCommessaCosti = totali;
        return moduloDocumento;
    }
}
