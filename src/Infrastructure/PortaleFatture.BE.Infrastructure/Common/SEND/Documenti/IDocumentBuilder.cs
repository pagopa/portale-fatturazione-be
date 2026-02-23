using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;
using PortaleFatture.BE.Core.Entities.SEND.Fatture;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Documenti
{
    public interface IDocumentBuilder
    {
        string? CreateEmailHtml(RelEmail dati);
        string? CreateModuloCommessaHtml(ModuloCommessaDocumentoDto dati);
        byte[] CreateModuloCommessaPdf(ModuloCommessaDocumentoDto dati);
        string? CreateModuloRelHtml(RelDocumentoDto dati);
        byte[] CreateModuloRelPdf(RelDocumentoDto dati);
        byte[] CreateDettaglioFatturaSospesaPdf(DocumentoContabileSospeso dati);
        string? CreateDettaglioFatturaSospesaHtml(DocumentoContabileSospeso dati);
        byte[] CreateDettaglioFatturaEmessaPdf(DocumentoContabileEmesso dati);
        byte[] CreateDettaglioFatturaEmessaMultiplaPdf(DocumentoContabileEmessiMultipli dati);
        string? CreateDettaglioFatturaEmessaHtml(DocumentoContabileEmesso dati);
        string? CreateDettaglioFatturaEmessaMultiplaHtml(DocumentoContabileEmessiMultipli dati);
    }
}