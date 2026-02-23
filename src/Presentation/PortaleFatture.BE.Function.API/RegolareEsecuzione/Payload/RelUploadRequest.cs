using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

public class RelUploadRequest
{
    [OpenApiProperty(Description = "Id Testata", Nullable = false)]
    public string? IdTestata { get; set; } 

    [OpenApiProperty(Description = "PDF firmato da caricare", Nullable = false)]
    public string File { get; set; } = "file (form field)";
} 