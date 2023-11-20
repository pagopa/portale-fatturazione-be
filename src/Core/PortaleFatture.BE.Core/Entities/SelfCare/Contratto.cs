using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.SelfCare
{
    [Table("Contratti")]
    public class Contratto
    {
        [Column("internalistitutionid")]
        public string? IdEnte { get; set; }
        [Column("product")]
        public string? Prodotto { get; set; }
        [Column("year")]
        public int AnnoValidita { get; set; }
        [Column("month")]
        public int MeseValidita { get; set; }
    }
} 