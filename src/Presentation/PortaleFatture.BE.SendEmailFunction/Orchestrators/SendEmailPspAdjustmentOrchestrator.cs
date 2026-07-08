using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture_BE_SendEmailFunction.Models.pagoPA;

namespace PortaleFatture_BE_SendEmailFunction.Orchestrators;

public class SendEmailPspAdjustmentOrchestrator
{
    [Function(nameof(SendEmailPspAdjustmentOrchestrator))]
    public async Task<string> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<EmailPspAdjustmentDataRequest>();
        await context.CallActivityAsync(nameof(SendEmailPspAdjustment), data);
        return "Email psp adjustment sent.";
    }
}