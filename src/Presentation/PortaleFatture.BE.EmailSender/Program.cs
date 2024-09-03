using System.Reflection;
using Microsoft.Extensions.Configuration;
using PortaleFatture.BE.Core.Entities.DatiRel.Dto;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.EmailSender.Models;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Services;
using PortaleFatture.BE.Infrastructure.Common.Documenti;
using PortaleFatture.BE.Infrastructure.Gateway.Email;

var risposta = new Risposta();
try
{
    IConfiguration config = new ConfigurationBuilder()
        .AddUserSecrets<Configurazione>()
        .Build();

    // config
    var connectionString = config.GetValue<string>("PortaleFattureOptions:ConnectionString");
    var from = config.GetValue<string>("PortaleFattureOptions:FROM");
    var smtp = config.GetValue<string>("PortaleFattureOptions:SMTP");
    var smtpPort = Convert.ToInt32(config.GetValue<string>("PortaleFattureOptions:SMTP_PORT"));
    var smtpAuth = config.GetValue<string>("PortaleFattureOptions:SMTP_AUTH");
    var smtpPassword = config.GetValue<string>("PortaleFattureOptions:SMTP_PASSWORD");

    var fileInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
    var path = fileInfo.Directory!.FullName;

    // params
    var anno = 2024;
    var mese = 6;
    var tipologiafattura = "PRIMO SALDO";
    var data = DateTime.UtcNow.ItalianTime().ToString("yyyy-MM-dd HH:mm:ss");
    var ricalcola = 0;

    Console.WriteLine("Ready to send");

    // response
    risposta = new Risposta()
    {
        Anno = anno,
        Mese = mese,
        TipologiaFattura = tipologiafattura,
        Data = data,
        Ricalcola = ricalcola
    };

    var builder = new DocumentBuilder(path);

    var subject = $"Notifica Regolare Esecuzione {tipologiafattura} Mese di {mese.GetMonth()}";
    var sender = new EmailSender(smtpSource: smtp!,
        smtpPort: smtpPort!,
        smtpUser: smtpAuth!,
        smtpPassword: smtpPassword!,
        from: from!);

    var emailService = new EmailRelService(connectionString!);

    // import csv
    List<RelEmail> enti = [];
    var values = Array.Empty<string>();
    var dir = $"Infrastructure/Documenti/";
    var fileEnti = "enti.csv";
    var filePath = Path.Combine([dir, fileEnti]);

    using (var reader = new StreamReader(filePath))
    {
        var count = 0;
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (count == 0)
            {
                count++;
                continue;
            }
            values = line!.Split(';');
            enti.Add(new RelEmail()
            {
                IdEnte = values[0],
                IdContratto = values[1],
                TipologiaFattura = values[2],
                Anno = Convert.ToInt32(values[3]),
                Mese = Convert.ToInt32(values[4]),
                Pec = values[5] == null || values[5] == "NULL" ? string.Empty : values[5],
                RagioneSociale = values[6],
            });
        }
    }

     //var dummy = 0;
    //foreach (var ente in enti!)
    //{
    //    var (msg, ver) = sender.SendEmail(ente.Pec!, subject, builder.CreateEmailHtml(ente)!);
    //    if (!ver)
    //        Console.WriteLine(msg);

    //    emailService.InsertTracciatoEmail(new RelEmailTracking()
    //    {
    //        Data = data,
    //        IdContratto = ente.IdContratto,
    //        Invio = Convert.ToByte(ver == true ? 1 : 0),
    //        Anno = ente.Anno,
    //        Mese = ente.Mese,
    //        Messaggio = msg,
    //        Pec = ente.Pec,
    //        IdEnte = ente.IdEnte,
    //        RagioneSociale = ente.RagioneSociale,
    //        TipologiaFattura = ente.TipologiaFattura
    //    });
    //}
}
catch (Exception ex)
{
    risposta.DbConnection = false;
    risposta.Error = ex.Message;
    Console.WriteLine(ex.Message);
}

Console.WriteLine(risposta.Serialize()); 