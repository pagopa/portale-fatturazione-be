using PortaleFatture.BE.Core.Entities.SEND.Fatture;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service
{
    public interface IServiceWorkFlowFatture
    {
        List<WorkFlowRequisitoFatture>? GetRequisiti();
        List<WorkFlowRequisitoFatture>? BiggerEqualThanCondition(int? condition);
    }
}