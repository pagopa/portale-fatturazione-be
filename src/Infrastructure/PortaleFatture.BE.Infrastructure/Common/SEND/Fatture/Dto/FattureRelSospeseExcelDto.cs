using System.ComponentModel.DataAnnotations.Schema;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

public class FattureRelSospeseExcelDto : FattureRelExcelDto
{
    [HeaderAttributev2(caption: "Rel Non Firmata", Order = 23)]
    [Column("RelNonFirmata")]
    public string? RelNonFirmata { get; set; }
}