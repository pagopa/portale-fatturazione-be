using PortaleFatture.BE.Core.Entities.Fatture;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Service
{
    public interface IServiceWorkFlowFatture
    {
        List<WorkFlowRequisitoFatture>? GetRequisiti();
        List<WorkFlowRequisitoFatture>? BiggerEqualThanCondition(int? condition);
    }
}