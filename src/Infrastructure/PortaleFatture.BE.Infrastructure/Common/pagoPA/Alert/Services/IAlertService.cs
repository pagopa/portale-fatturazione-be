using PortaleFatture.BE.Infrastructure.Common.pagoPA.Alert.Dto;


namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.Alert.Services
{
    public interface IAlertService
    {
        (AlertDto,List<string>?) GetAlert(int IdAlert);
        bool InsertTracciatoEmail(AlertTracking email);
    }
}