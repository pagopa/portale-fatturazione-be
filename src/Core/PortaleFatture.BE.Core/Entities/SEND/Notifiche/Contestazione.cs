using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.SEND.Notifiche;

[Table("Contestazioni")]
public class Contestazione
{
    [Column("IdContestazione")]
    public int Id { get; set; }

    [Column("FkIdTipoContestazione")]
    public int TipoContestazione { get; set; }

    [Column("FkIdNotifica")]
    public string? IdNotifica { get; set; }

    [Column("NoteEnte")]
    public string? NoteEnte { get; set; }

    [Column("NoteSend")]
    public string? NoteSend { get; set; }

    [Column("NoteRecapitista")]
    public string? NoteRecapitista { get; set; }

    [Column("NoteConsolidatore")]
    public string? NoteConsolidatore { get; set; }

    [Column("RispostaEnte")]
    public string? RispostaEnte { get; set; }

    [Column("FkIdFlagContestazione")]
    public short StatoContestazione { get; set; }

    [Column("Onere")]
    public string? Onere { get; set; }

    [Column("DataInserimentoEnte")]
    public DateTime DataInserimentoEnte { get; set; }

    [Column("DataModificaEnte")]
    public DateTime? DataModificaEnte { get; set; }

    [Column("DataInserimentoSend")]
    public DateTime? DataInserimentoSend { get; set; }

    [Column("DataModificaSend")]
    public DateTime? DataModificaSend { get; set; }

    [Column("DataInserimentoRecapitista")]
    public DateTime? DataInserimentoRecapitista { get; set; }

    [Column("DataModificaRecapitista")]
    public DateTime? DataModificaRecapitista { get; set; }

    [Column("DataInserimentoConsolidatore")]
    public DateTime? DataInserimentoConsolidatore { get; set; }

    [Column("DataModificaConsolidatore")]
    public DateTime? DataModificaConsolidatore { get; set; }

    [Column("DataChiusura")]
    public DateTime? DataChiusura { get; set; }

    [Column("Anno")]
    public int Anno { get; set; }

    [Column("Mese")]
    public int Mese { get; set; }
}