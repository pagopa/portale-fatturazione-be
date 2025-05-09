using System.Net;
using System.Text;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
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
            if (httpRequestData.Body.CanRead)
            {
                using var memoryStream = new MemoryStream();
                await httpRequestData.Body.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                using var reader = new StreamReader(memoryStream, Encoding.UTF8);
                var bodyString = await reader.ReadToEndAsync();
                context.SetItem("requestBody", bodyString);
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
        var authorizedIpsList = authorizedIps?.Select(x => x.IpAddress).ToList();

        if (authorizedIpsList.IsNullNotAny() || !authorizedIpsList!.Contains(ipAddress))
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
        context.SetItem("RequestId", context.InvocationId);

        await next(context);
    }

    private async Task<ApiKeyDto?> IsValidApiKey(string apiKey)
    {
        try
        {
            return await _mediator.Send(new ApiKeyQueryGetByKey() { ApiKey = apiKey });
        }
        catch(Exception ex)
        {
            _logger.LogError($"{LoggerHelper.PREFIX}***{MessageHelper.Unauthorized}{LoggerHelper.PIPE}{ex.Serialize()}");
            return null;
        }
    }
}