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
        string? CreateDettaglioFatturaEmessaHtml(DocumentoContabileEmesso dati);

        /// <summary>
        /// Genera un documento PDF che rappresenta il dettaglio di una o più fatture emesse, utilizzando i dati
        /// forniti.
        /// </summary>
        /// <param name="dati">I dati delle fatture emesse da includere nel PDF. Non può essere null.</param>
        /// <returns>Un array di byte che contiene il file PDF generato. L'array sarà vuoto se non sono presenti dati validi.</returns>
        byte[] CreateDettaglioFatturaEmessaMultiplaPdf(DocumentoContabileEmessiMultipli dati);

        /// <summary>
        /// Genera il contenuto HTML dettagliato per una o più fatture emesse, utilizzando i dati forniti.
        /// </summary>
        /// <param name="dati">I dati delle fatture emesse da rappresentare in formato HTML. Non può essere null.</param>
        /// <returns>Una stringa contenente il markup HTML dettagliato delle fatture emesse, oppure null se i dati non sono
        /// validi o non disponibili.</returns>
        string? CreateDettaglioFatturaEmessaMultiplaHtml(DocumentoContabileEmessiMultipli dati);
    }
}