using PortaleFatture.BE.Function.API.Autenticazione.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands;

namespace PortaleFatture.BE.Function.API.Autenticazione.Extensions;

public static class AutenticazioneExtensions
{
    public static AuthenticationResponse Map(this CreateApyLogCommand command)
    {
        return new AuthenticationResponse
        {
            IdEnte = command.FkIdEnte,
            Timestamp = command.Timestamp,
            FunctionName = command.FunctionName,
            Stage = command.Stage,
            Method = command.Method, 
            Uri = command.Uri,
            IpAddress = command.IpAddress,
            Id = command.Id
        };
    } 
} 