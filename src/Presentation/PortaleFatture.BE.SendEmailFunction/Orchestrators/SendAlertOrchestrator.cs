using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture_BE_SendEmailFunction.Models;
using PortaleFatture_BE_SendEmailFunction.Models.Alert;

namespace PortaleFatture_BE_SendEmailFunction.Orchestrators;

public class SendAlertOrchestrator
{
    [Function("SendAlertOrchestrator")]
    public async Task<RispostaAlert?> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<AlertDataRequest>();
        return await context.CallActivityAsync<RispostaAlert?>("SendAlert", data);        
    }
}