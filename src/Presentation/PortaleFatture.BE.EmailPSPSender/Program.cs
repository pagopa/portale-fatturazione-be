using Microsoft.Extensions.Configuration;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.EmailPSPSender.Models;
using PortaleFatture.BE.Infrastructure.Gateway.Email;

IConfiguration config = new ConfigurationBuilder()
    .AddUserSecrets<Configurazione>()
    .Build();

var accessToken = config.GetValue<string>("AccessToken");
var refreshToken = config.GetValue<string>("RefreshToken");
var clientId = config.GetValue<string>("ClientId");
var clientSecret = config.GetValue<string>("ClientSecret");
var from = config.GetValue<string>("From");
var fromName = config.GetValue<string>("FromName");
var to = config.GetValue<string>("To");
var toName = config.GetValue<string>("ToName");

var sender = new PspEmailSender(accessToken, refreshToken, clientId, clientSecret, from, fromName);

var subject = "test";
var body = "<h1>test</h1>";
var applicationName = "whatever";
var (msg, result) = sender.SendEmail(to!, toName!, subject, body, applicationName);

var dictionary = new Dictionary<string, string>(capacity: 10);
var resultFolder = sender.SentFolder(dictionary, applicationName);

Console.WriteLine(resultFolder.Serialize());
Console.WriteLine(msg);
Console.WriteLine(result);
Console.ReadKey();