using System.Data;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Asseverazione.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.Asseverazione.Extensions;

public static class AsseverazioneExtensions
{
    public static List<EnteAsserverazioneImportDto> Mapper(this DataTable dt)
    {
        var list = new List<EnteAsserverazioneImportDto>();
        foreach (DataRow row in dt.Rows)
        {
            var ente = new EnteAsserverazioneImportDto
            {
                IdEnte = row[0].ToString(),
                RagioneSociale = row[1].ToString(),
                DataAsseverazione = String.IsNullOrEmpty(row[2].ToString()) ? null : Convert.ToDateTime(row[2]),
                TipoAsseverazione = String.IsNullOrEmpty(row[3].ToString()) ? null : row[3].ToString() == "SI"
            };
            list.Add(ente);
        }
        return list;
    }

    public static EnteAsserverazioneListImportCreateCommand Mapper(this DataTable dt, AuthenticationInfo? authInfo)
    {
        var timestamp = DateTime.UtcNow.ItalianTime();
        var command = new EnteAsserverazioneListImportCreateCommand(authInfo);
        var list = new List<EnteAsserverazioneImportCreateCommand>();
        var provider = CultureInfo.InvariantCulture;
        string[] formats = { "yyyy-MM-dd", "yyyy/MM/dd", "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd 00:00:00", "yyyy/MM/dd 00:00:00", "dd-MM-yyyy 00:00:00", "dd/MM/yyyy 00:00:00" };
       
        foreach (DataRow row in dt.Rows)
        { 
                var ente = new EnteAsserverazioneImportCreateCommand(timestamp)
                {
                    IdEnte = row[0].ToString(),
                    RagioneSociale = row[1].ToString(),
                    DataAsseverazione = String.IsNullOrEmpty(row[2].ToString()) ? null : DateTime.ParseExact(row[2].ToString()!, formats, provider), // Convert.ToDateTime(row[2]),
                    TipoAsseverazione = String.IsNullOrEmpty(row[3].ToString()) ? null : row[3].ToString() == "SI",
                    Descrizione = row[4].ToString(),
                    IdUtente = authInfo?.Id!
                };

                if (ente.TipoAsseverazione.HasValue)
                    list.Add(ente); 
        }
        command.ListCommands = list;
        return command;
    }
}