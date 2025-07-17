using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture_BE_SendEmailFunction.Models;

namespace PortaleFatture_BE_SendEmailFunction.Orchestrators;

public class RichiestaNotificheOrchestrator
{
    [Function("RichiestaNotificheOrchestrator")]
    public async Task<string> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<NotificheRicercaRequest>();
        data!.InstanceId = context.InstanceId;
        await context.CallActivityAsync("RichiestaNotifiche", data); 
        return "Created Richiesta Notifiche";
    }
}