using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Services
{
    public interface IEmailRelService
    {
        IEnumerable<RelEmail>? GetSenderEmail(int? anno, int? mese, string tipologiaFattura);
        bool InsertTracciatoEmail(RelEmailTracking email);
    }
}