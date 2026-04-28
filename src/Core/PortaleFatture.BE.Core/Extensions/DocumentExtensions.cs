using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using PortaleFatture.BE.Core.Entities.SEND.DatiFatturazioni;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;
using PortaleFatture.BE.Core.Entities.SEND.Fatture;

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

    private static string _tableDettaglioMesiHeader = @"
        <table width='100%' style='border-collapse: collapse; font-family: Calibri, sans-serif; font-size: 12pt;'>
            <tr style='background-color: #f2f2f2;'>
                <th style='border: 1px solid black; padding: 6px 8px; text-align: left; vertical-align: middle;'>Mese</th>
                <th style='border: 1px solid black; padding: 6px 8px; text-align: center; vertical-align: middle;'>Notifiche Digitali</th>
                <th style='border: 1px solid black; padding: 6px 8px; text-align: center; vertical-align: middle; white-space: nowrap;'>Importo<br/>Notifiche Digitali</th>
                <th style='border: 1px solid black; padding: 6px 8px; text-align: center; vertical-align: middle;'>Notifiche Analogiche</th>
                <th style='border: 1px solid black; padding: 6px 8px; text-align: center; vertical-align: middle; white-space: nowrap;'>Importo<br/>Notifiche Analogiche</th>
                <th style='border: 1px solid black; padding: 6px 8px; text-align: center; vertical-align: middle; white-space: nowrap;'>Importo</th>
            </tr>
    ";

    private static string _tableDettaglioMesiRow = @"
            <tr>
                <td style='border: 1px solid black; padding: 6px 8px; text-align: left; vertical-align: middle;'>[Mese]/[Anno]</td>
                <td style='border: 1px solid black; padding: 6px 8px; text-align: center; vertical-align: middle;'>[TotaleNotificheDigitali]</td>
                <td style='border: 1px solid black; padding: 6px 8px; text-align: right; vertical-align: middle; white-space: nowrap;'>[TotaleDigitale]&nbsp;€</td>
                <td style='border: 1px solid black; padding: 6px 8px; text-align: center; vertical-align: middle;'>[TotaleNotificheAnalogiche]</td>
                <td style='border: 1px solid black; padding: 6px 8px; text-align: right; vertical-align: middle; white-space: nowrap;'>[TotaleAnalogico]&nbsp;€</td>
                <td style='border: 1px solid black; padding: 6px 8px; text-align: right; vertical-align: middle; white-space: nowrap;'>[Totale]&nbsp;€</td>
            </tr> 
    ";

    private static string _tableDettaglioMesiFooter = @"</table>";

    public static string GetDettaglioMesi(this IEnumerable<DocumentoContabileEmesso>? fatture)
    {
        if (fatture == null || !fatture.Any()) return string.Empty;

        StringBuilder builder = new();

        foreach (var fattura in fatture)
        {
            var row = _tableDettaglioMesiRow
                .Replace("[Mese]", fattura.Mese.PadLeft(2, '0'))
                .Replace("[Anno]", fattura.Anno)
                .Replace("[TotaleNotificheDigitali]", fattura.TotaleNotificheDigitali.ToString())
                .Replace("[TotaleNotificheAnalogiche]", fattura.TotaleNotificheAnalogiche.ToString())
                .Replace("[TotaleDigitale]", fattura.TotaleDigitale.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
                .Replace("[TotaleAnalogico]", fattura.TotaleAnalogico.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
                .Replace("[Totale]", fattura.Totale!.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")));
            builder.Append(row);
        }

        builder.Append(_tableDettaglioMesiFooter);
        return builder.ToString();
    }

    public static string GetDettaglioMesi(this IEnumerable<DocumentoContabileEmessoRiepilogo>? fatture, bool showDetails = false)
    {
        if (fatture == null || !fatture.Any()) return string.Empty;

        StringBuilder builder = new();
        foreach (var fattura in fatture)
        {
            var isPac = fattura.TipologiaContratto != null &&
                        fattura.TipologiaContratto.Contains("PAC", StringComparison.InvariantCultureIgnoreCase);

            // ? TODO: va bene gestirlo solo per le emesse ?
            var tipologiaFattura = fattura.TipologiaFatturaSospesa ?? fattura.TipologiaFattura;
            if (tipologiaFattura == "SECONDO SALDO")
            {
                tipologiaFattura = $"{tipologiaFattura} Contestazioni risolte";
            }
            else if (tipologiaFattura == "PRIMO SALDO")
            {
                tipologiaFattura = string.Empty;
            }

            const string baseStyle = "font-family: Calibri, sans-serif;font-style: normal;font-weight: normal;text-decoration: none;font-size: 16pt;padding-top: 9pt; text-indent: 0pt; line-height: 114%; text-align: justify;";
            const string listStyle = "font-family: Calibri, sans-serif;font-style: normal;font-weight: normal;text-decoration: none;font-size: 16pt;padding-right: 0pt;margin-left: 24pt;line-height: 114%;text-align: justify;list-style-type: disc;";
            const string listItemStyle = "padding-top: 9pt;";
            const string boldSpanStyle = "font-family: Calibri, sans-serif;font-style: normal;font-weight: bold;text-decoration: none;font-size: 16pt;";
            //const string avoidBreakStyle = "page-break-inside: avoid; break-inside: avoid; -webkit-column-break-inside: avoid;";
            //const string bulletStyle = "font-family: Calibri, sans-serif;font-style: normal;font-weight: normal;text-decoration: none;font-size: 16pt;padding-top: 9pt; padding-left: 16pt; text-indent: 0pt; line-height: 114%; text-align: justify;";
            const string avoidBreakStyle = "page-break-inside: avoid; break-inside: avoid; -webkit-column-break-inside: avoid; padding-top: 6pt;";

            var tipologiaSuffix = string.IsNullOrWhiteSpace(tipologiaFattura) ? string.Empty : $" {tipologiaFattura}";

            var totale = (fattura.Totale ?? 0).ToString("N2", CultureInfo.CreateSpecificCulture("it-IT"));
            var totaleDigitale = (fattura.TotaleDigitale ?? 0).ToString("N2", CultureInfo.CreateSpecificCulture("it-IT"));
            var totaleAnalogico = (fattura.TotaleAnalogico ?? 0).ToString("N2", CultureInfo.CreateSpecificCulture("it-IT"));
            var totaleSospesoImponibile = (fattura.TotaleFatturaSospesaImponibile ?? 0).ToString("N2", CultureInfo.CreateSpecificCulture("it-IT"));

            builder.Append($"<div style=\"{avoidBreakStyle}\">");
            builder.Append($"<p style=\"{baseStyle}\">MESE DI {fattura.Mese.PadLeft(2, '0')}/{fattura.Anno}{tipologiaSuffix}</p>");

            builder.Append($"<ul style=\"{listStyle}\">");
            builder.Append($"<li style=\"{listItemStyle}\">€ <span>{totale}</span> oltre IVA, così determinato: € {totaleDigitale} per n.&nbsp;{fattura.TotaleNotificheDigitali ?? 0} notifiche digitali e € {totaleAnalogico} per n.&nbsp;{fattura.TotaleNotificheAnalogiche ?? 0} notifiche analogiche espletate.</li>");
            builder.Append("</ul>");

            if (showDetails)
            {
                builder.Append($"<p style=\"{baseStyle}\">{(isPac ? "Agli importi sopra indicati per l’emissione di regolare fattura sono stornati i seguenti importi già versati in anticipo e/o acconto:" : "Agli importi sopra indicati per l’emissione di regolare fattura sono stornati i seguenti importi già versati in anticipo:")}</p>");

                builder.Append($"<ul style=\"{listStyle}\">");
                builder.Append($"<li style=\"{listItemStyle}\">€ {fattura.TotaleAnticipo.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT"))} oltre IVA, così determinato: € {fattura.AnticipoDigitale.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT"))} per l’anticipazione delle notifiche digitali e € {fattura.AnticipoAnalogico.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT"))} per l’anticipazione delle notifiche analogiche.</li>");

                if (isPac)
                {
                    builder.Append($"<li style=\"{listItemStyle}\">€ {fattura.TotaleAcconto.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT"))} oltre IVA, così determinato: € {fattura.AccontoDigitale.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT"))} per l’acconto delle notifiche digitali e € {fattura.AccontoAnalogico.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT"))} per l’acconto delle notifiche analogiche.</li>");
                }
                builder.Append("</ul>");
            }
            else
            {
                builder.Append($"<p style=\"{baseStyle}\">{(isPac ? "Agli importi sopra indicati per l’emissione di regolare fattura sono stornati gli importi già versati in anticipo e/o acconto." : "Agli importi sopra indicati per l’emissione di regolare fattura sono stornati gli importi già versati in anticipo.")}</p>");
            }
            builder.Append($"<p>Fattura imponibile pari a <span style=\"{boldSpanStyle}\">{totaleSospesoImponibile}</span> oltre IVA</p>");
            builder.Append("</div>");
 
        }

        return builder.ToString();
    }

    public static string GetElencoMesi(this IEnumerable<DocumentoContabileEmesso>? fatture)
    {
        if (fatture == null || !fatture.Any()) return string.Empty;
        
        var mesi = fatture.Select(f => $"{f.Mese.PadLeft(2, '0')}/{f.Anno}").Distinct().ToList();
        return string.Join(", ", mesi);
    }

    public static string GetElencoMesi(this IEnumerable<DocumentoContabileEmessoRiepilogo>? fatture)
    {
        if (fatture == null || !fatture.Any()) return string.Empty;
        
        var mesi = fatture.Select(f => $"{f.Mese.PadLeft(2, '0')}/{f.Anno}").Distinct().ToList();
        return string.Join(", ", mesi);
    }

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
            .Replace(nameof(model.Totale).GetName<RelDocumentoDto>(), decimal.Parse(model.Totale!).ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
            .Replace(nameof(model.TotaleAnalogico).GetName<RelDocumentoDto>(), decimal.Parse(model.TotaleAnalogico!).ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
            .Replace(nameof(model.TotaleDigitale).GetName<RelDocumentoDto>(), decimal.Parse(model.TotaleDigitale!).ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
            .Replace(nameof(model.TotaleNotificheAnalogiche).GetName<RelDocumentoDto>(), model.TotaleNotificheAnalogiche!.ToString())
            .Replace(nameof(model.TotaleNotificheDigitali).GetName<RelDocumentoDto>(), model.TotaleNotificheDigitali!.ToString())
            .Replace(nameof(model.TipologiaFattura).GetName<RelDocumentoDto>(), model.TipologiaFattura!)
            .Replace(nameof(model.RagioneSociale).GetName<RelDocumentoDto>(), model.RagioneSociale!)
            .Replace(nameof(model.IdContratto).GetName<RelDocumentoDto>(), model.IdContratto!)
              ;

        return template;
    }

    public static string Replace(this DocumentoContabileSospeso model, string template)
    {
        template = template
            .Replace(nameof(model.Anno).GetName<DocumentoContabileSospeso>(), model.Anno.ToString())
            .Replace(nameof(model.Mese).GetName<DocumentoContabileSospeso>(), model.Mese.ToString())
            .Replace("[ElencoMesi]", $"{model.Mese.ToString().PadLeft(2, '0')}/{model.Anno}")
            .Replace(nameof(model.Totale).GetName<DocumentoContabileSospeso>(), model.Totale.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
            .Replace(nameof(model.TotaleAnalogico).GetName<DocumentoContabileSospeso>(), model.TotaleAnalogico.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
            .Replace(nameof(model.AnticipoAnalogico).GetName<DocumentoContabileSospeso>(), model.AnticipoAnalogico.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
            .Replace(nameof(model.AnticipoDigitale).GetName<DocumentoContabileSospeso>(), model.AnticipoDigitale.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
            .Replace(nameof(model.TotaleStorno).GetName<DocumentoContabileSospeso>(), model.TotaleStorno.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
            .Replace(nameof(model.StornoDigitale).GetName<DocumentoContabileSospeso>(), model.StornoDigitale.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
            .Replace(nameof(model.StornoAnalogico).GetName<DocumentoContabileSospeso>(), model.StornoAnalogico.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
            .Replace(nameof(model.ImportoSottoSoglia).GetName<DocumentoContabileSospeso>(), model.ImportoSottoSoglia.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
            .Replace(nameof(model.TotaleDigitale).GetName<DocumentoContabileSospeso>(), model.TotaleDigitale.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
            .Replace(nameof(model.TotaleNotificheAnalogiche).GetName<DocumentoContabileSospeso>(), model.TotaleNotificheAnalogiche.ToString())
            .Replace(nameof(model.TotaleNotificheDigitali).GetName<DocumentoContabileSospeso>(), model.TotaleNotificheDigitali.ToString())
            .Replace(nameof(model.TipologiaFattura).GetName<DocumentoContabileSospeso>(), model.TipologiaFattura ?? string.Empty)
            .Replace(nameof(model.RagioneSociale).GetName<DocumentoContabileSospeso>(), model.RagioneSociale ?? string.Empty)
            .Replace(nameof(model.IdContratto).GetName<DocumentoContabileSospeso>(), model.IdContratto ?? string.Empty)
              ;

        return template;
    }

    public static string Replace(this DocumentoContabileEmesso model, string template)
    {
        template = template
            .Replace(nameof(model.Anno).GetName<DocumentoContabileEmesso>(), model.Anno)
            .Replace(nameof(model.Mese).GetName<DocumentoContabileEmesso>(), model.Mese)
            .Replace("[ElencoMesi]", $"{model.Mese.PadLeft(2, '0')}/{model.Anno}")
            .Replace("[TipologiaFattura]", model.TipologiaFattura ?? string.Empty)
            .Replace(nameof(model.Totale).GetName<DocumentoContabileEmesso>(), model.Totale.ToString("N2", CultureInfo.CreateSpecificCulture("it-IT")))
            .Replace(nameof(model.TotaleAnalogico).GetName<DocumentoContabileEmesso>(), model.TotaleAnalogico.ToString("N2"))
            .Replace(nameof(model.TotaleDigitale).GetName<DocumentoContabileEmesso>(), model.TotaleDigitale.ToString("N2"))
            .Replace(nameof(model.TotaleNotificheAnalogiche).GetName<DocumentoContabileEmesso>(), model.TotaleNotificheAnalogiche.ToString())
            .Replace(nameof(model.TotaleNotificheDigitali).GetName<DocumentoContabileEmesso>(), model.TotaleNotificheDigitali.ToString())
            .Replace(nameof(model.TotaleAnticipo).GetName<DocumentoContabileEmesso>(), model.TotaleAnticipo.ToString("N2"))
            .Replace(nameof(model.AnticipoDigitale).GetName<DocumentoContabileEmesso>(), model.AnticipoDigitale.ToString("N2"))
            .Replace(nameof(model.AnticipoAnalogico).GetName<DocumentoContabileEmesso>(), model.AnticipoAnalogico.ToString("N2"))
            .Replace(nameof(model.TotaleStorno).GetName<DocumentoContabileEmesso>(), model.TotaleStorno.ToString("N2"))
            .Replace(nameof(model.StornoDigitale).GetName<DocumentoContabileEmesso>(), model.StornoDigitale.ToString("N2"))
            .Replace(nameof(model.StornoAnalogico).GetName<DocumentoContabileEmesso>(), model.StornoAnalogico.ToString("N2"))
            .Replace(nameof(model.TotaleAcconto).GetName<DocumentoContabileEmesso>(), model.TotaleAcconto.ToString("N2"))
            .Replace(nameof(model.AccontoDigitale).GetName<DocumentoContabileEmesso>(), model.AccontoDigitale.ToString("N2"))
            .Replace(nameof(model.AccontoAnalogico).GetName<DocumentoContabileEmesso>(), model.AccontoAnalogico.ToString("N2"))
            .Replace(nameof(model.Imponibile).GetName<DocumentoContabileEmesso>(), model.Imponibile.ToString("N2"))
            .Replace(nameof(model.RagioneSociale).GetName<DocumentoContabileEmesso>(), model.RagioneSociale!)
            .Replace(nameof(model.IdContratto).GetName<DocumentoContabileEmesso>(), model.IdContratto!)
              ;

        return template;
    }


    /// <summary>
    /// Sostituisce i placeholder nel modello di testo specificato con i valori correnti delle proprietà dell'istanza di
    /// DocumentoContabileEmessiMultipli.
    /// </summary>
    /// <remarks>I placeholder devono corrispondere ai nomi delle proprietà di
    /// DocumentoContabileEmessiMultipli, formattati secondo la convenzione utilizzata dal metodo GetName. I valori
    /// numerici sono formattati come numeri con due decimali, ove applicabile. I segnaposto non riconosciuti rimangono
    /// invariati.</remarks>
    /// <param name="model">L'istanza di DocumentoContabileEmessiMultipli i cui valori vengono utilizzati per sostituire i segnaposto nel
    /// modello.</param>
    /// <param name="template">La stringa di modello contenente i segnaposto da sostituire con i valori delle proprietà.</param>
    /// <returns>Una nuova stringa in cui tutti i segnaposto riconosciuti sono stati sostituiti con i valori corrispondenti
    /// dell'istanza fornita.</returns>
    public static string Replace(this DocumentoContabileEmessiMultipli model, string template)
    {
        template = template
            .Replace(nameof(model.Totale).GetName<DocumentoContabileEmessiMultipli>(), model.Totale.ToString("N2"))
            .Replace(nameof(model.TotaleAnalogico).GetName<DocumentoContabileEmessiMultipli>(), model.TotaleAnalogico.ToString("N2"))
            .Replace(nameof(model.TotaleDigitale).GetName<DocumentoContabileEmessiMultipli>(), model.TotaleDigitale.ToString("N2"))
            .Replace(nameof(model.TotaleNotificheAnalogiche).GetName<DocumentoContabileEmessiMultipli>(), model.TotaleNotificheAnalogiche.ToString())
            .Replace(nameof(model.TotaleNotificheDigitali).GetName<DocumentoContabileEmessiMultipli>(), model.TotaleNotificheDigitali.ToString())
            .Replace(nameof(model.TotaleAnticipo).GetName<DocumentoContabileEmessiMultipli>(), model.TotaleAnticipo.ToString("N2"))
            .Replace(nameof(model.AnticipoDigitale).GetName<DocumentoContabileEmessiMultipli>(), model.AnticipoDigitale.ToString("N2"))
            .Replace(nameof(model.AnticipoAnalogico).GetName<DocumentoContabileEmessiMultipli>(), model.AnticipoAnalogico.ToString("N2"))
            .Replace(nameof(model.TotaleStorno).GetName<DocumentoContabileEmessiMultipli>(), model.TotaleStorno.ToString("N2"))
            .Replace(nameof(model.StornoDigitale).GetName<DocumentoContabileEmessiMultipli>(), model.StornoDigitale.ToString("N2"))
            .Replace(nameof(model.StornoAnalogico).GetName<DocumentoContabileEmessiMultipli>(), model.StornoAnalogico.ToString("N2"))
            .Replace(nameof(model.TotaleAcconto).GetName<DocumentoContabileEmessiMultipli>(), model.TotaleAcconto.ToString("N2"))
            .Replace(nameof(model.AccontoDigitale).GetName<DocumentoContabileEmessiMultipli>(), model.AccontoDigitale.ToString("N2"))
            .Replace(nameof(model.AccontoAnalogico).GetName<DocumentoContabileEmessiMultipli>(), model.AccontoAnalogico.ToString("N2"))
            .Replace(nameof(model.Imponibile).GetName<DocumentoContabileEmessiMultipli>(), model.Imponibile.ToString("N2"))
            .Replace(nameof(model.RagioneSociale).GetName<DocumentoContabileEmessiMultipli>(), model.RagioneSociale!)
            .Replace(nameof(model.IdContratto).GetName<DocumentoContabileEmessiMultipli>(), model.IdContratto!)
            .Replace("[DettaglioMesi]", model.DettaglioFatture.GetDettaglioMesi(false))
            .Replace("[DettaglioMesiV2]", model.DettaglioFatture.GetDettaglioMesi(true))
            .Replace("[ElencoMesi]", model.DettaglioFatture.GetElencoMesi())
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
            .Replace(nameof(model.Semestre).GetName<RelEmail>(), model.Semestre)
            .Replace("[ElencoMesi]", model.ElencoMesi ?? string.Empty)
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

        if(!(totaleModulo == null || totaleModulo.Totali.IsNullNotAny()))
        {
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
        } 
        return moduloDocumento;
    }
}
