using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Extensions;


namespace PortaleFatture.BE.Infrastructure.Common.SEND.Banner.Dto;

public class BannerDto
{
    public Guid Id { get; set; }
    public DateTime DataInizio { get; set; }
    public DateTime DataFine { get; set; }
    public string? Testo { get; set; }
    public bool Visibile { get; set; }
    
}
