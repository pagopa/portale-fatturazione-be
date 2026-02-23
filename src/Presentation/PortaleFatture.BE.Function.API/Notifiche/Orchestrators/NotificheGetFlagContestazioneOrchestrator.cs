using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Function.API.Notifiche.Payload;

namespace PortaleFatture.BE.Function.API.Notifiche.Orchestrators;

public class NotificheGetFlagContestazioneOrchestrator
{
    [Function("NotificheGetFlagContestazioneOrchestrator")]
    public async Task<IEnumerable<FlagContestazione>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<NotificheRicercaInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<FlagContestazione>>("NotificheGetFlagContestazione", data); ;
    }
}