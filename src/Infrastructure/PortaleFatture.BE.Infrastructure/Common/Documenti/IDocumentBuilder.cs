using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Entities.DatiRel.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.Documenti
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