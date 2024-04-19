using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;

namespace PortaleFatture.BE.Core.Entities.Notifiche;

public static class SoggettiContestazione
{
    public static string OnereContestazioneAccettazionePA(string soggetto)
    {
        if (soggetto == Profilo.PubblicaAmministrazione)
            return $"SEND_{Profilo.PubblicaAmministrazione}";
        else if (soggetto == Profilo.GestorePubblicoServizio)
            return $"SEND_{Profilo.GestorePubblicoServizio}";
        else if (soggetto == Profilo.SocietaControlloPubblico)
            return $"SEND_{Profilo.SocietaControlloPubblico}";
        else if (soggetto == Profilo.PrestatoreServiziPagamento)
            return $"SEND_{Profilo.PrestatoreServiziPagamento}";
        else if (soggetto == Profilo.AssicurazioniIVASS)
            return $"SEND_{Profilo.AssicurazioniIVASS}";
        else if (soggetto == Profilo.StazioneAppaltanteANAC)
            return $"SEND_{Profilo.StazioneAppaltanteANAC}";
        else if (soggetto == Profilo.PartnerTecnologico)
            return $"SEND_{Profilo.PartnerTecnologico}";
        else
            throw new DomainException("");
    } 
  
    public static string OnereContestazioneChiusuraPA(string soggetto)
    {
        if (soggetto == Profilo.Consolidatore)
            return $"SEND_{Profilo.Consolidatore}";
        else if (soggetto == Profilo.Recapitista)
            return $"SEND_{Profilo.Recapitista}"; 
        else if (soggetto == "SEND")
            return $"SEND_SEND";
        else
            throw new DomainException("");
    }

    public static string OnereContestazioneChiusuraEnte(string soggetto, string profilo)
    {
        if (soggetto == Profilo.Consolidatore && profilo == Profilo.PubblicaAmministrazione)
            return $"{Profilo.PubblicaAmministrazione}_{Profilo.Consolidatore}";
        else if (soggetto == Profilo.Consolidatore && profilo == Profilo.GestorePubblicoServizio)
            return $"{Profilo.GestorePubblicoServizio}_{Profilo.Consolidatore}";
        else if (soggetto == Profilo.Consolidatore || profilo == Profilo.SocietaControlloPubblico)
            return $"{Profilo.SocietaControlloPubblico}_{Profilo.Consolidatore}";
        else if (soggetto == Profilo.Consolidatore || profilo == Profilo.PrestatoreServiziPagamento)
            return $"{Profilo.PrestatoreServiziPagamento}_{Profilo.Consolidatore}";
        else if (soggetto == Profilo.Consolidatore || profilo == Profilo.AssicurazioniIVASS)
            return $"{Profilo.PubblicaAmministrazione}_{Profilo.Consolidatore}";
        else if (soggetto == Profilo.Consolidatore || profilo == Profilo.StazioneAppaltanteANAC)
            return $"{Profilo.StazioneAppaltanteANAC}_{Profilo.Consolidatore}";
        else if (soggetto == Profilo.Consolidatore || profilo == Profilo.StazioneAppaltanteANAC)
            return $"{Profilo.StazioneAppaltanteANAC}_{Profilo.Consolidatore}";
        else if (soggetto == Profilo.Consolidatore || profilo == Profilo.PartnerTecnologico)
            return $"{Profilo.PartnerTecnologico}_{Profilo.Consolidatore}";

        else if (soggetto == Profilo.Recapitista && profilo == Profilo.PubblicaAmministrazione)
            return $"{Profilo.PubblicaAmministrazione}_{Profilo.Recapitista}";
        else if (soggetto == Profilo.Recapitista && profilo == Profilo.GestorePubblicoServizio)
            return $"{Profilo.GestorePubblicoServizio}_{Profilo.Recapitista}";
        else if (soggetto == Profilo.Recapitista || profilo == Profilo.SocietaControlloPubblico)
            return $"{Profilo.SocietaControlloPubblico}_{Profilo.Recapitista}";
        else if (soggetto == Profilo.Recapitista || profilo == Profilo.PrestatoreServiziPagamento)
            return $"{Profilo.PrestatoreServiziPagamento}_{Profilo.Recapitista}";
        else if (soggetto == Profilo.Recapitista || profilo == Profilo.AssicurazioniIVASS)
            return $"{Profilo.PubblicaAmministrazione}_{Profilo.Recapitista}";
        else if (soggetto == Profilo.Recapitista || profilo == Profilo.StazioneAppaltanteANAC)
            return $"{Profilo.StazioneAppaltanteANAC}_{Profilo.Recapitista}";
        else if (soggetto == Profilo.Recapitista || profilo == Profilo.StazioneAppaltanteANAC)
            return $"{Profilo.StazioneAppaltanteANAC}_{Profilo.Recapitista}";
        else if (soggetto == Profilo.Recapitista || profilo == Profilo.PartnerTecnologico)
            return $"{Profilo.PartnerTecnologico}_{Profilo.Recapitista}";

        else if (soggetto == "SEND" && profilo == Profilo.PubblicaAmministrazione)
            return $"{Profilo.PubblicaAmministrazione}_SEND";
        else if (soggetto == "SEND" && profilo == Profilo.GestorePubblicoServizio)
            return $"{Profilo.GestorePubblicoServizio}_SEND";
        else if (soggetto == "SEND" || profilo == Profilo.SocietaControlloPubblico)
            return $"{Profilo.SocietaControlloPubblico}_SEND";
        else if (soggetto == "SEND" || profilo == Profilo.PrestatoreServiziPagamento)
            return $"{Profilo.PrestatoreServiziPagamento}_SEND";
        else if (soggetto == "SEND" || profilo == Profilo.AssicurazioniIVASS)
            return $"{Profilo.PubblicaAmministrazione}_SEND";
        else if (soggetto == "SEND" || profilo == Profilo.StazioneAppaltanteANAC)
            return $"{Profilo.StazioneAppaltanteANAC}_SEND";
        else if (soggetto == "SEND" || profilo == Profilo.StazioneAppaltanteANAC)
            return $"{Profilo.StazioneAppaltanteANAC}_SEND";
        else if (soggetto == "SEND" || profilo == Profilo.PartnerTecnologico)
            return $"{Profilo.PartnerTecnologico}_SEND";
        else
            throw new DomainException("");
    }
}