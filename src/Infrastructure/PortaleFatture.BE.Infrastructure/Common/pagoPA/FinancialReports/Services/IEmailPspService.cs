using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Services
{
    public interface IEmailPspService
    {
        IEnumerable<PspEmail>? GetSenderEmail(string? trimestre);
        bool InsertTracciatoEmail(PspEmailTracking email);
    }
}