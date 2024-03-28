using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Core.Entities.DatiRel.Dto;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Extensions;

public static class DatiRelInfraExtensions
{
    public static RelUploadStateCreateCommand Map(this RelUploadCreateCommand command)
    {
        return new RelUploadStateCreateCommand(command.AuthenticationInfo)
        {
            Anno = command.Anno,
            IdContratto = command.IdContratto,
            IdEnte = command.IdEnte,
            Mese = command.Mese,
            TipologiaFattura = command.TipologiaFattura
        };
    }

    public static RelUploadCreateCommand Map(this RelDownloadCommand command)
    {
        return new RelUploadCreateCommand(command.AuthenticationInfo)
        {
            Anno = command.Anno,
            IdContratto = command.IdContratto,
            IdEnte = command.IdEnte,
            Mese = command.Mese,
            TipologiaFattura = command.TipologiaFattura,
            Azione = command.Azione,
            DataEvento = command.DataEvento,
            IdUtente = command.IdUtente,
            Hash = command.Hash
        };
    }

    public static List<RelUploadCreateCommand> Map(this List<RelDownloadCommand> commands)
    {
        List<RelUploadCreateCommand> uploads = [];
        foreach (var command in commands)
        {
            uploads.Add(new RelUploadCreateCommand(command.AuthenticationInfo)
            {
                Anno = command.Anno,
                IdContratto = command.IdContratto,
                IdEnte = command.IdEnte,
                Mese = command.Mese,
                TipologiaFattura = command.TipologiaFattura,
                Azione = command.Azione,
                DataEvento = command.DataEvento,
                IdUtente = command.IdUtente,
                Hash = command.Hash
            }); 
        }
        return uploads;
    }

    public static RelBulkDownloadCommand Map(this IEnumerable<SimpleRelTestata>? testate, Dictionary<string, string> values, AuthenticationInfo info)
    {
        RelBulkDownloadCommand command = new(info);
        var adesso = DateTime.UtcNow.ItalianTime();
        command.Commands = [];
        foreach (var testata in testate!)
        {
            values.TryGetValue(testata.IdTestata!, out var hash);
            command.Commands.Add(new RelDownloadCommand(info)
            {
                Anno = testata.Anno,
                IdContratto = testata.IdContratto,
                IdEnte = testata.IdEnte,
                Mese = testata.Mese,
                TipologiaFattura = testata.TipologiaFattura,
                Azione = RelAzioneDocumento.Zip,
                DataEvento = adesso,
                IdUtente = info.Id,
                Hash = hash
            });
        }
        return command;
    }
}