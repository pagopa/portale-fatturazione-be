using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture_BE_SendEmailFunction.Models.pagoPA;

namespace PortaleFatture_BE_SendEmailFunction.Orchestrators;

public class SendEmailPspOrchestrator
{
    [Function("SendEmailPspOrchestrator")]
    public async Task<string> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<EmailPspDataRequest>();
        await context.CallActivityAsync("SendEmailPsp", data);
        return "Email psp sent.";
    }
}