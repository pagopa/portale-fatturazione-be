using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture_BE_SendEmailFunction.Models;

namespace PortaleFatture_BE_SendEmailFunction.Orchestrators;

public class CreateRelRigheOrchestrator
{
    [Function("CreateRelRigheOrchestrator")]
    public async Task<string> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<EmailRelDataRequest>();
        await context.CallActivityAsync("CreateRelRighe", data);
        return "Created Rel Righe";
    }
}