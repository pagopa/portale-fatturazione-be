using System.Net;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Models;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries;

namespace PortaleFatture.BE.Function.API.Middleware;

public class AuthMiddleware(
    ILogger<AuthMiddleware> logger,
    IConfigurazione configurazione,
    IMediator mediator) : IFunctionsWorkerMiddleware
{
    private readonly ILogger _logger = logger;
    private readonly IMediator _mediator = mediator;
    private readonly IConfigurazione _configurazione = configurazione;
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequestData = await context.GetHttpRequestDataAsync();
        context.SetItem("requestBody", httpRequestData);
        if (httpRequestData != null && httpRequestData.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            if (httpRequestData.Headers.TryGetValues("Content-Type", out var contentTypes))
            {
                var contentType = contentTypes.FirstOrDefault();

                if (contentType != null && contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                {

                    var uploadFile = await ProcessMultipartAndProvideInfo(httpRequestData, context);
                    if (uploadFile != null)
                    {
                        context.SetItem("UploadFile", uploadFile);
                    } 
                }
                else if (httpRequestData.Body.CanRead)
                {
                    using var memoryStream = new MemoryStream();
                    await httpRequestData.Body.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    using var reader = new StreamReader(memoryStream, Encoding.UTF8);
                    var bodyString = await reader.ReadToEndAsync();
                    context.SetItem("requestBody", bodyString);
                }
            }
        }


        if (!context.FunctionDefinition.InputBindings.TryGetValue("req", out var binding) || binding.Type != "httpTrigger")
        {
            await next(context);
            return;
        }

        if (httpRequestData == null)
        {
            await next(context);
            return;
        }

        if (httpRequestData.SkipSwagger())
        {
            await next(context);
            return;
        }

        var apiKey = httpRequestData.GetApiKey();

        if (string.IsNullOrEmpty(apiKey))
        {
            var response = httpRequestData?.CreateResponse(HttpStatusCode.Unauthorized);
            if (response == null)
                return;

            _logger.LogInformation($"{LoggerHelper.PREFIX}*{MessageHelper.Unauthorized}{LoggerHelper.PIPE}{httpRequestData.Serialize()}");
            await response.WriteStringAsync(MessageHelper.Unauthorized);
            return;
        }

        var resultQueryApiKey = await IsValidApiKey(apiKey);
        if (resultQueryApiKey == null)
        {
            var response = httpRequestData.CreateResponse(HttpStatusCode.Unauthorized);
            _logger.LogInformation($"{LoggerHelper.PREFIX}**{MessageHelper.Unauthorized}{LoggerHelper.PIPE}{httpRequestData.Serialize()}");
            await response.WriteStringAsync(MessageHelper.Unauthorized);
            return;
        }

        var ipAddress = httpRequestData.ExtractIpAddress();

        _logger.LogInformation($"{LoggerHelper.PREFIX}{"IP Original Address: "}{LoggerHelper.PIPE}{ipAddress}");

        var authorizedIps = await _mediator.Send(new ApiKeyIpsQueryGet(new AuthenticationInfo() { IdEnte = resultQueryApiKey.IdEnte }));
        if (authorizedIps.IsNullNotAny())
        {
            var response = httpRequestData.CreateResponse(HttpStatusCode.Forbidden);
            _logger.LogInformation($"{LoggerHelper.PREFIX}{MessageHelper.Forbidden}{LoggerHelper.PIPE}{httpRequestData.Serialize()}");
            await response.WriteStringAsync(MessageHelper.Forbidden);
            return;
        }

        var authorizedIpsList = new List<string>();

        foreach (var ip in authorizedIps!)
        {
            if (ip.IpAddress!.Contains('/'))
            {
                var network = IPNetwork.Parse(ip.IpAddress);
                var realIp = IPAddress.Parse(ipAddress!);
                if (network.Contains(realIp))
                {
                    authorizedIpsList.Add(ip.IpAddress);
                }
            }
            else
            {
                if (ip.IpAddress == ipAddress)
                    authorizedIpsList.Add(ip.IpAddress);
            }
        }

        if (authorizedIpsList.IsNullNotAny())
        {
            var response = httpRequestData.CreateResponse(HttpStatusCode.Forbidden);
            _logger.LogInformation($"{LoggerHelper.PREFIX}{MessageHelper.Forbidden}{LoggerHelper.PIPE}{httpRequestData.Serialize()}");
            await response.WriteStringAsync(MessageHelper.Forbidden);
            return;
        }

        context.SetItem("IdEnte", resultQueryApiKey.IdEnte!);
        context.SetItem("FunctionName", context.FunctionDefinition?.Name ?? "UnknownFunction");
        context.SetItem("IpAddress", ipAddress!);
        context.SetItem("ApiKey", resultQueryApiKey.ApiKey!);
        context.SetItem("Uri", _configurazione.GetUri(httpRequestData.Url?.ToString()));
        context.SetItem("Stage", ScopeType.REQUEST);
        context.SetItem("Payload", await httpRequestData.ReadAsStringAsync());
        context.SetItem("Method", httpRequestData.Method);
        context.SetItem("IdContratto", resultQueryApiKey.IdContratto);
        context.SetItem("IdTipoContratto", resultQueryApiKey.IdTipoContratto);
        context.SetItem("RagioneSociale", resultQueryApiKey.RagioneSociale);
        context.SetItem("Prodotto", resultQueryApiKey.Prodotto);
        context.SetItem("RequestId", Guid.NewGuid().ToString());
        context.SetItem("Profilo", resultQueryApiKey.Profilo);
        await next(context);
    }

    private async Task<ApiKeyEnteDto?> IsValidApiKey(string apiKey)
    {
        try
        {
            return await _mediator.Send(new ApiKeyQueryGetByKeyEnte() { ApiKey = apiKey });
        }
        catch (Exception ex)
        {
            _logger.LogError($"{LoggerHelper.PREFIX}{MessageHelper.Unauthorized}{LoggerHelper.PIPE}{ex.Serialize()}");
            return null;
        }
    }

    private async Task<FileUploadDto?> ProcessMultipartAndProvideInfo(HttpRequestData httpRequestData, FunctionContext context)
    {
        var contentTypeHeader = httpRequestData.Headers.GetValues("Content-Type").FirstOrDefault();
        if (!MediaTypeHeaderValue.TryParse(contentTypeHeader, out var mediaTypeHeader))
        {
            _logger.LogError("Error: Invalid Content-Type header.");
            return null;
        }

        var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary).Value;
        if (string.IsNullOrEmpty(boundary))
        {
            _logger.LogError("Error: Multipart boundary not found.");
            return null;
        }

        using var requestBodyBuffer = new MemoryStream();
        await httpRequestData.Body.CopyToAsync(requestBodyBuffer);
        requestBodyBuffer.Position = 0;

        var reader = new MultipartReader(boundary, requestBodyBuffer);
        var dto = new FileUploadDto();
        MultipartSection section;
        while ((section = await reader.ReadNextSectionAsync()) != null)
        {
            if (section.Headers != null && section.Headers.TryGetValue("Content-Disposition", out var contentDispositionValues))
            {
                var contentDispositionValue = contentDispositionValues.FirstOrDefault();
                if (contentDispositionValue != null && ContentDispositionHeaderValue.TryParse(contentDispositionValue, out var contentDisposition))
                {
                    if (contentDisposition.DispositionType == "form-data" && contentDisposition.FileName.HasValue)
                    {
                        dto.FileName = contentDisposition.FileName.Value.Trim('"');
                        if (contentDisposition?.Name.HasValue == true)
                            dto.FormFieldName = contentDisposition.Name.Value.Trim('"');

                        dto.FileContentType = section.Headers.TryGetValue("Content-Type", out var typeValues) ? typeValues.FirstOrDefault() : "application/octet-stream";

                        using var ms = new MemoryStream();
                        await section.Body.CopyToAsync(ms);
                        dto.FileBytes = ms.ToArray();

                        _logger.LogInformation($"Middleware extracted file: {dto.FileName} (Field: {dto.FormFieldName}, Type: {dto.FileContentType}, Size: {dto.FileBytes.Length} bytes)");
                    }
                    else if (contentDisposition.DispositionType == "form-data" && contentDisposition.Name.HasValue)
                    {
                        using var streamReader = new StreamReader(section.Body, Encoding.UTF8);
                        dto.FormTextValue = await streamReader.ReadToEndAsync();
                        _logger.LogInformation($"Middleware extracted form field: {contentDisposition.Name.Value.Trim('"')} = {dto.FormTextValue}");
                    }
                }
            }
        }
        return dto;
    }
}