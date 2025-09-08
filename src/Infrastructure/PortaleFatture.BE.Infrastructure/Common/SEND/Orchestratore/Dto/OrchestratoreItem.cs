using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Dto;

public sealed class OrchestratoreItem
{
    [HeaderAttributev2(caption: "Anno", Order = 1)]
    public int Anno { get; set; }

    [HeaderAttributev2(caption: "Mese", Order = 2)]
    public int Mese { get; set; }

    [HeaderAttributev2(caption: "Tipologia", Order = 3)]
    public string? Tipologia { get; set; }

    [HeaderAttributev2(caption: "Fase", Order = 4)]
    public string? Fase { get; set; }

    [HeaderAttributev2(caption: "Data Esecuzione", Order = 5)]
    public DateTime? DataEsecuzione { get; set; }

    [HeaderAttributev2(caption: "Data Fine Contestazioni", Order = 6)]
    public DateTime? DataFineContestazioni { get; set; }

    [HeaderAttributev2(caption: "Data Chiusura Contestazioni", Order = 7)]
    public DateTime? ChiusuraContestazioni { get; set; }

    [HeaderAttributev2(caption: "Data Tempo Risposta", Order = 8)]
    public DateTime? TempoRisposta { get; set; } 

    [HeaderAttributev2(caption: "Data Fatturazione", Order = 9)]
    public DateTime? DataFatturazione { get; set; }

    public int? Esecuzione { get; set; }

    [HeaderAttributev2(caption: "Count", Order = 10)]
    public int? Count { get; set; }

    [HeaderAttributev2(caption: "Esecuzione", Order = 11)]
    public string? DescrizioneEsecuzione
    {
        get
        {
            return StatiQuery.GetStati().TryGetValue(Esecuzione!.Value, out var description)
                ? description
                : null;
        }
    } 
}