﻿using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.DatiFatturazioni;

[Table("DatiFatturazioneContatti")]
public class DatiFatturazioneContatto
{ 
    [Column("FkIdDatiFatturazione")]
    public long IdDatiFatturazione { get; set; }
    public string? Email { get; set; } 
} 