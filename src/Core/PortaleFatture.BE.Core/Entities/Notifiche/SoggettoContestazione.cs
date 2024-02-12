using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;

namespace PortaleFatture.BE.Core.Entities.Notifiche;

public static class SoggettiContestazione
{
    public static string OnereContestazioneAccettazionePA(string soggetto)
    {
        if (soggetto == Profilo.PubblicaAmministrazione)
            return $"SEND_{Profilo.PubblicaAmministrazione}";
        else
            throw new DomainException("");
    }
    public static string OnereContestazione(string soggetto)
    {
        if (soggetto == Profilo.Consolidatore)
            return $"SEND_{Profilo.Consolidatore}";
        else if (soggetto == Profilo.Recapitista)
            return $"SEND_{Profilo.Recapitista}"; 
        else
            throw new DomainException("");
    }

    public static string OnereContestazioneChiusuraEnte(string soggetto)
    {
        if (soggetto == Profilo.Consolidatore)
            return $"{Profilo.PubblicaAmministrazione}_{Profilo.Consolidatore}";
        else if (soggetto == Profilo.Recapitista)
            return $"{Profilo.PubblicaAmministrazione}_{Profilo.Recapitista}";
        else if (soggetto == "SEND")
            return $"{Profilo.PubblicaAmministrazione}_SEND";
        else
            throw new DomainException("");
    }
}