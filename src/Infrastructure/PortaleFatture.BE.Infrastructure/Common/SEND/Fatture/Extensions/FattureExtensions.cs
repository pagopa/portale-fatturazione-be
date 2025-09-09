using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.Fatture;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service;
namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Extensions;

public static class FattureExtensions
{
    public static DataTable CreateRipristinoFattureTable(this long[]? idFatture)
    {
        var table = new DataTable();

        // The UDTT only needs IdFattura column
        table.Columns.Add("IdFattura", typeof(long));

        if (idFatture != null)
        {
            foreach (var id in idFatture)
            {
                table.Rows.Add(id);
            }
        }

        return table;
    }

    public static FattureCommessaEliminateExcelQuery Map(this FattureCommessaExcelQuery command)
    {
        return new FattureCommessaEliminateExcelQuery(command.AuthenticationInfo)
        {
            Anno = command.Anno,
            IdEnti = command.IdEnti,
            Mese = command.Mese
        };
    }

    public static FattureIdsQueryByParameters Map(this FatturaRiptristinoSAPCommand command)
    {
        return new FattureIdsQueryByParameters(command.AuthenticationInfo!,
            command.Anno,
            command.Mese,
            command.TipologiaFattura,
            command.FatturaInviata,
            command.StatoAtteso);
    }

    public static List<FatturaDistinctDto> ToWorkFlow(this FattureListaDto? fatture, IServiceWorkFlowFatture _workFlowService)
    {
        var distinctFatture = fatture!
            .GroupBy(f => f.fattura!.TipologiaFattura)
            .Select(g => new FatturaDistinctDto
            {
                TipologiaFattura = g.First().fattura!.TipologiaFattura!,
                Mese = Convert.ToInt32(g.First().fattura!.Identificativo!.Split("/")[0]),
                Anno = Convert.ToInt32(g.First().fattura!.Identificativo!.Split("/")[1]),
            }).ToList();


        foreach (var d in distinctFatture)
        {
            Dictionary<string, List<WorkFlowRequisitoFatture>?>? workFlow = [];
            var condition = _workFlowService.GetRequisiti()!.Where(x => x.TipologiaFattura == d.TipologiaFattura).Select(x => x.Condition).FirstOrDefault();
            var extracondition = _workFlowService.GetRequisiti()!.Where(x => x.TipologiaFattura == d.TipologiaFattura).Select(x => x.ExtraCondition).FirstOrDefault();
            var conditions = _workFlowService.BiggerEqualThanCondition(condition);
            workFlow.Add(d.TipologiaFattura!, conditions);
            d.WorkFlow = workFlow;
        }

        return distinctFatture;
    }

    public static List<FatturaDistinctDto> ToWorkFlow(this IEnumerable<FatturaInvioSap>? fatture, IServiceWorkFlowFatture _workFlowService)
    {
        var distinctFatture = fatture!
            .GroupBy(f => f!.TipologiaFattura)
            .Select(g => new FatturaDistinctDto
            {
                TipologiaFattura = g.First()!.TipologiaFattura!,
                Mese = g.First().MeseRiferimento,
                Anno = g.First().AnnoRiferimento
            }).ToList();


        foreach (var d in distinctFatture)
        {
            Dictionary<string, List<WorkFlowRequisitoFatture>?>? workFlow = [];
            var condition = _workFlowService.GetRequisiti()!.Where(x => x.TipologiaFattura == d.TipologiaFattura).Select(x => x.Condition).FirstOrDefault();
            var extracondition = _workFlowService.GetRequisiti()!.Where(x => x.TipologiaFattura == d.TipologiaFattura).Select(x => x.ExtraCondition).FirstOrDefault();
            var conditions = _workFlowService.BiggerEqualThanCondition(condition);
            workFlow.Add(d.TipologiaFattura!, conditions);
            d.WorkFlow = workFlow;
        }

        return distinctFatture;
    }
}