﻿using System.Runtime.Serialization;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Commands;

public class DatiModuloCommessaTotaleCreateCommand : IRequest<DatiModuloCommessaTotale>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public int AnnoValidita { get; set; }
    public int MeseValidita { get; set; }
    public string? IdEnte { get; set; }
    public long IdTipoContratto { get; set; }
    public string? Stato { get; set; }
    public string? Prodotto { get; set; }
    public int IdCategoriaSpedizione { get; set; }
    public decimal TotaleCategoria { get; set; }
    public int PercentualeCategoria { get; set; }
    public decimal Totale { get; set; }
    public bool Fatturabile { get; set; } = true;
}