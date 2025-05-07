using System.Net;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;

namespace PortaleFatture.BE.Function.API.Middleware;

public class LogCustomDataMiddleware(
    ILogger<LogCustomDataMiddleware> logger,
    IMediator mediator) : IFunctionsWorkerMiddleware
{
    private readonly ILogger _logger = logger;
    private readonly IMediator _mediator = mediator;

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequestData = await context.GetHttpRequestDataAsync();
        if (httpRequestData == null)
            return; 

        if (httpRequestData.SkipSwagger())
        {
            await next(context);
            return;
        } 

        try
        { 
            await _mediator.Send(context.Request());
        }
        catch
        {
            var response = httpRequestData.CreateResponse(HttpStatusCode.BadRequest); 
            _logger.LogInformation($"{LoggerHelper.PREFIX}{MessageHelper.BadRequestLogging}{LoggerHelper.PIPE}{context.Serialize()}");
            await response.WriteStringAsync(MessageHelper.BadRequestLogging);
            return;
        }
        await next(context);
    }
}
