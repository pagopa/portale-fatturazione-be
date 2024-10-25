using Microsoft.AspNetCore.Mvc;

namespace PortaleFatture.BE.Api.Infrastructure.Documenti;

public sealed class DisposableStreamResult(Stream stream, string contentType) : FileStreamResult(stream, contentType)
{
    public override Task ExecuteResultAsync(ActionContext context)
    {
        return base.ExecuteResultAsync(context).ContinueWith(task =>
        {
            if (stream == null) 
                return;
            stream.Dispose();
        });
    }
}