namespace PortaleFatture.BE.Function.API.Models;

public class FileUploadDto
{
    public string? FileName { get; set; }
    public byte[]? FileBytes { get; set; }
    public string? FormFieldName { get; set; }
    public string? FileContentType { get; set; }
    public string? FormTextValue { get; set; }
}