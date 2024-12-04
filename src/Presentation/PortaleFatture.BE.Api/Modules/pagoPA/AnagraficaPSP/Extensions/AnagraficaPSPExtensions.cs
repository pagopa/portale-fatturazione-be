using PortaleFatture.BE.Api.Modules.pagoPA.AnagraficaPSP.Request;
using PortaleFatture.BE.Api.Modules.pagoPA.FinancialReports.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries;

namespace PortaleFatture.BE.Api.Modules.pagoPA.AnagraficaPSP.Extensions;

public static class AnagraficaPSPExtensions
{
    public static PSPsQuartersRequestQuery Map(this PSPsQuartersRequest req, AuthenticationInfo authInfo)
    {
        return new PSPsQuartersRequestQuery(authInfo)
        {
            Year = req.Year
        };
    } 

    public static PSPQueryGetByRicerca Map(this PSPRequest req, AuthenticationInfo authInfo)
    {
        return new PSPQueryGetByRicerca(authInfo)
        {
            ContractIds = req.ContractIds.IsNullNotAny() ? null : req.ContractIds,
            MembershipId = req.MembershipId,
            RecipientId = req.RecipientId,
            ABI = req.ABI,
            YearQuarter = req.Quarters,
        };
    }

    public static PSPQueryGetByRicerca Map(this PSPRequest req, AuthenticationInfo authInfo, int? page, int? pageSize)
    {
        return new PSPQueryGetByRicerca(authInfo)
        {
            Page = page,
            Size = pageSize,
            ContractIds = req.ContractIds.IsNullNotAny() ? null : req.ContractIds,
            MembershipId = req.MembershipId,
            RecipientId = req.RecipientId,
            ABI = req.ABI,
            YearQuarter = req.Quarters,
        };
    }

    public static PSPQueryGetByName Map(this PSPRequestName req, AuthenticationInfo authInfo)
    {
        return new PSPQueryGetByName(authInfo)
        {
            Name = req.Name, 
            YearQuarter = req.Quarters,
        };
    }
}