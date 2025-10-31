using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Gateway.ModuloCommessa;
using PortaleFatture_BE_SendEmailFunction.Models;
namespace PortaleFatture_BE_SendEmailFunction;

public class ModuloCommessaPrevisionale(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ModuloCommessaPrevisionale>();

    [Function("ModuloCommessaPrevisionale")]
    public static async Task<HttpResponseData> RunHttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "modulocommessa")]
         HttpRequestData req,
         FunctionContext context)
    {
        var logger = context.GetLogger("ModuloCommessaPrevisionale");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<ModuloCommessaRequest>(requestBody);

            if (data is null || String.IsNullOrEmpty(data!.DownloadUrl))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("DownloadUrl non può essere null o vuoto");
                return badResponse;
            }

            var downloadUrl = data!.DownloadUrl;

            ConfigurazioneModuloCommessa.ModuloCommessaSEND = GetEnvironmentVariable("ModuloCommessaSEND");
            ConfigurazioneModuloCommessa.ModuloCommessaSENDAccountKey = GetEnvironmentVariable("ModuloCommessaSENDAccountKey");
            ConfigurazioneModuloCommessa.ModuloCommessaSENDFileVersion = GetEnvironmentVariable("ModuloCommessaSENDFileVersion");

            if (String.IsNullOrEmpty(ConfigurazioneModuloCommessa.ModuloCommessaSEND) ||
                String.IsNullOrEmpty(ConfigurazioneModuloCommessa.ModuloCommessaSENDAccountKey) ||
                String.IsNullOrEmpty(ConfigurazioneModuloCommessa.ModuloCommessaSENDFileVersion))
            {
                var configResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await configResponse.WriteStringAsync("Fornire una uri ModuloCommessaSEND e un ModuloCommessaSENDAccountKey e un ModuloCommessaSENDFileVersion");
                return configResponse;
            }

            var httpClient = new HttpModuloCommessaEventClient(ConfigurazioneModuloCommessa.ModuloCommessaSENDAccountKey, ConfigurazioneModuloCommessa.ModuloCommessaSEND);
            var response = await httpClient.SendFileReadyEventAsync(downloadUrl!, ConfigurazioneModuloCommessa.ModuloCommessaSENDFileVersion);

            if (response.IsSuccessStatusCode)
            {
                var successResponse = req.CreateResponse(HttpStatusCode.OK);
                await successResponse.WriteStringAsync("true");
                return successResponse;
            }
            else
            {
                logger.LogError(response.Serialize());
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("false");
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore durante l'elaborazione della richiesta");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Errore: {ex.Message}");
            return errorResponse;
        }
    }
    private static string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }
}