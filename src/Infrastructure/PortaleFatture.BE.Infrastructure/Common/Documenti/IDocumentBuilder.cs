using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.Documenti
{
    public interface IDocumentBuilder
    {
        byte[] CreateModuloCommessaPdf(ModuloCommessaDocumentoDto dati); 
        string? CreateModuloCommessaHtml(ModuloCommessaDocumentoDto dati);
    }
}