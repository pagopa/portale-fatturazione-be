using PortaleFatture.BE.Core.Entities.DatiRel.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Services
{
    public interface IEmailRelService
    {
        IEnumerable<RelEmail>? GetSenderEmail(int? anno, int? mese, string tipologiaFattura);
        bool InsertTracciatoEmail(RelEmailTracking email);
    }
}