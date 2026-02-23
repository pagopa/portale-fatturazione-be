using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture_BE_SendEmailFunction.Models;

namespace PortaleFatture_BE_SendEmailFunction.Orchestrators;

public class CreateRelSospeseOrchestrator
{
    [Function("CreateRelSospeseOrchestrator")]
    public async Task<string> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        try
        {
            var data = context.GetInput<EmailRelDataRequest>();
            var result = await context.CallActivityAsync<string>("CreateRelSospese", data);
            return result;
        }
        catch (TaskFailedException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            throw new InvalidOperationException(
                $"CreateRelSospese fallita: {innerMessage}", ex.InnerException ?? ex);
        }
    }
}