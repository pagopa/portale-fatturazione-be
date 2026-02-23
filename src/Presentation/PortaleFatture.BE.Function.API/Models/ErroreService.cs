using Microsoft.Extensions.Localization;

namespace PortaleFatture.BE.Function.API.Models;

public class ErroreService
{
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public ErroreService(IStringLocalizer<ErrorMessages> localizer)
    {
        _localizer = localizer;
    }

    public string GetMessaggioErrore(string key)
    {
        return _localizer[key];
    }
}
 
public class ErrorMessages { }
