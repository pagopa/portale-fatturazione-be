using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture_BE_SendEmailFunction.Models.pagoPA;

namespace PortaleFatture_BE_SendEmailFunction.Orchestrators;

public class SendEmailPspAdjustmentOrchestrator
{
    [Function(nameof(SendEmailPspAdjustmentOrchestrator))]
    public async Task<RispostapagoPA> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<EmailPspAdjustmentDataRequest>();
        var risposta = await context.CallActivityAsync<RispostapagoPA>(nameof(SendEmailPspAdjustment), data);
        return risposta;
    }
}