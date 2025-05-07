using System.Net;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Autenticazione.Extensions;
using PortaleFatture.BE.Function.API.Autenticazione.Payload;
using PortaleFatture.BE.Function.API.Extensions;

namespace PortaleFatture.BE.Function.API.Autenticazione;

public static class Authentication
{
    [Function("Authentication")] 
    [OpenApiOperation(operationId: "authenticateUser", tags: ["authentication"], Summary = "Authenticates a user.", Description = "Authenticates a user based on provided credentials.")]
    [OpenApiSecurity("ApiKeyAuth", SecuritySchemeType.ApiKey, Name = "X-API-KEY", In = OpenApiSecurityLocationType.Header)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AuthenticationResponse), Description = "Returns 'Auth Response' on successful authentication.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    public static async Task<HttpResponseData> RunHttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/authentication")] 
        HttpRequestData req,
        FunctionContext context, 
        ILogger logger)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        var logResponse =  context.Response();
        try
        {
            await mediator.Send(logResponse);
        }
        catch(Exception ex)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            logger.LogInformation($"{LoggerHelper.PREFIX}{MessageHelper.BadRequestLogging}{LoggerHelper.PIPE}{ex.Serialize()}");
            await badResponse.WriteStringAsync(MessageHelper.BadRequestLogging);
            return badResponse;
        }
        var successResponse = req.CreateResponse(HttpStatusCode.OK);
        await successResponse.WriteAsJsonAsync(logResponse.Map()); 
        return successResponse;
    }
} 