using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Documenti
{
    public interface IDocumentBuilder
    {
        string? CreateEmailHtml(RelEmail dati);
        string? CreateModuloCommessaHtml(ModuloCommessaDocumentoDto dati);
        byte[] CreateModuloCommessaPdf(ModuloCommessaDocumentoDto dati);
        string? CreateModuloRelHtml(RelDocumentoDto dati);
        byte[] CreateModuloRelPdf(RelDocumentoDto dati);
    }
}