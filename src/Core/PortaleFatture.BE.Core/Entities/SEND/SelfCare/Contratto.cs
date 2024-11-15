﻿using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.SEND.SelfCare
{
    [Table("Contratti")]
    public class Contratto
    {
        [Column("internalistitutionid")]
        public string? IdEnte { get; set; }
        [Column("product")]
        public string? Prodotto { get; set; }
        [Column("FkIdTipoContratto")]
        public long IdTipoContratto { get; set; }
    }
}