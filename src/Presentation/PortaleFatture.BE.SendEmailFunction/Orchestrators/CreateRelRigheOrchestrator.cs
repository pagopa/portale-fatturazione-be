using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture_BE_SendEmailFunction.Models;

namespace PortaleFatture_BE_SendEmailFunction.Orchestrators;

public class CreateRelRigheOrchestrator
{
    [Function("CreateRelRigheOrchestrator")]
    public async Task<string> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        try
        {
            var data = context.GetInput<EmailRelDataRequest>();
            var result = await context.CallActivityAsync<string>("CreateRelRighe", data);
            return result;
        }
        //catch (DomainException ex)
        //{ 
        //    throw new InvalidOperationException(
        //        $"L'attività 'CreateRelRighe' è fallita con errore: {ex.Message}", ex);
        //}
        catch (TaskFailedException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            throw new InvalidOperationException(
                $"CreateRelRighe fallita: {innerMessage}", ex.InnerException ?? ex);
        }
    }
}