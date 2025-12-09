using System.Text;
using PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiFatturazioni;
using PortaleFatture.BE.Core.Entities.SEND.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Commands;

namespace PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Extensions;
public static class DatiFatturazioneExtensions
{
    public static byte[] ToCsv(this IEnumerable<DatiFatturazioneEnteDto> dati)
    {
        var csv = new StringBuilder();
        foreach (var d in dati)
        {
            var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", d.RagioneSociale, d.Prodotto, d.Profilo, d.Cup, d.CodCommessa, d.IdDocumento, d.DataDocumento, d.SplitPayment, d.Pec, d.NotaLegale, d.DataCreazione, d.DataModifica);
            csv.AppendLine(newLine);
        }
        return Encoding.ASCII.GetBytes(csv.ToString());
    }

    public static string GenerateFakeIdEnte()
    {
        return Guid.NewGuid().ToString();
    }

    public static DatiFatturazioneContattoCreateCommand Mapper(this DatiFatturazioneContattoCreateRequest model) =>
       new()
       {
           Email = model.Email
       };

    public static DatiFatturazioneCreateCommand Mapper(this DatiFatturazionePagoPACreateRequest model, AuthenticationInfo authInfo)
    {
        authInfo.Prodotto = model.Prodotto;
        authInfo.IdEnte = model.IdEnte;
        return new DatiFatturazioneCreateCommand(authInfo)
        {
            NotaLegale = model.NotaLegale,
            CodCommessa = model.CodCommessa,
            Cup = model.Cup,
            DataDocumento = model.DataDocumento,
            SplitPayment = model.SplitPayment,
            IdDocumento = model.IdDocumento,
            Map = model.Map,
            Contatti = model.Contatti?.Select(x => x.Mapper()).ToList(),
            Pec = model.Pec,
            TipoCommessa = model.TipoCommessa,
            CodiceSDI = model.CodiceSDI
        };
    }

    public static DatiFatturazioneCreateCommand Mapper(this DatiFatturazioneCreateRequest model, AuthenticationInfo authInfo) =>
       new(authInfo)
       {
           NotaLegale = model.NotaLegale,
           CodCommessa = model.CodCommessa,
           Cup = model.Cup,
           DataDocumento = model.DataDocumento,
           SplitPayment = model.SplitPayment,
           IdDocumento = model.IdDocumento,
           Map = model.Map,
           Contatti = model.Contatti?.Select(x => x.Mapper()).ToList(),
           Pec = model.Pec,
           TipoCommessa = model.TipoCommessa,
           CodiceSDI = model.CodiceSDI
       };

    public static DatiFatturazioneUpdateCommand Mapper(this DatiFatturazionePagoPAUpdateRequest model, AuthenticationInfo authInfo)
    {
        authInfo.Prodotto = model.Prodotto;
        authInfo.IdEnte = model.IdEnte;
        return new(authInfo)
        {
            Id = model.Id,
            NotaLegale = model.NotaLegale,
            CodCommessa = model.CodCommessa,
            Cup = model.Cup,
            DataDocumento = model.DataDocumento,
            SplitPayment = model.SplitPayment,
            IdDocumento = model.IdDocumento,
            Map = model.Map,
            Contatti = model.Contatti?.Select(x => x.Mapper()).ToList(),
            Pec = model.Pec,
            TipoCommessa = model.TipoCommessa, 
            CodiceSDI = model.CodiceSDI
        };

    }

    public static DatiFatturazioneUpdateCommand Mapper(this DatiFatturazioneUpdateRequest model, AuthenticationInfo authInfo) =>
       new(authInfo)
       {
           Id = model.Id,
           NotaLegale = model.NotaLegale,
           CodCommessa = model.CodCommessa,
           Cup = model.Cup,
           DataDocumento = model.DataDocumento,
           SplitPayment = model.SplitPayment,
           IdDocumento = model.IdDocumento,
           Map = model.Map,
           Contatti = model.Contatti?.Select(x => x.Mapper()).ToList(),
           Pec = model.Pec,
           TipoCommessa = model.TipoCommessa,
           CodiceSDI = model.CodiceSDI
       };

    public static DatiFatturazioneContattoResponse Mapper(this DatiFatturazioneContatto model) =>
       new()
       {
           Email = model.Email
       };

    public static DatiFatturazioneResponse Mapper(this DatiFatturazione model, string? codiceSDI = "") =>
       new()
       {
           NotaLegale = model.NotaLegale,
           CodCommessa = model.CodCommessa,
           Cup = model.Cup,
           DataDocumento = model.DataDocumento,
           SplitPayment = model.SplitPayment,
           IdDocumento = model.IdDocumento,
           Map = model.Map,
           IdEnte = model.IdEnte,
           Id = model.Id,
           DataCreazione = model.DataCreazione.DateTime,
           DataModifica = model.DataModifica != null ? model.DataModifica!.Value.DateTime : null,
           Pec = model.Pec,
           TipoCommessa = model.TipoCommessa,
           Prodotto = model.Prodotto,
           Contatti = model.Contatti?.Select(x => x.Mapper()),
           CodiceSDI = model.CodiceSDI,
           ContractCodiceSDI = codiceSDI
       };

    public static TipoContrattoResponse Mapper(this TipoContratto model) =>
       new()
       {
           Id = model.Id,
           Descrizione = model.Descrizione
       };

    public static IEnumerable<TipoContrattoResponse> Mapper(this IEnumerable<TipoContratto> models)
    {
        return models.Select(x => x.Mapper());
    }


    public static TipoCommessaResponse Mapper(this TipoCommessa model) =>
       new()
       {
           Id = model.Id,
           Descrizione = model.Descrizione
       };

    public static IEnumerable<TipoCommessaResponse> Mapper(this IEnumerable<TipoCommessa> models)
    {
        return models.Select(x => x.Mapper());
    }

    public static ProdottoResponse Mapper(this Prodotto model) =>
           new()
           {
               Nome = model.Nome
           };

    public static IEnumerable<ProdottoResponse> Mapper(this IEnumerable<Prodotto> models)
    {
        return models.Select(x => x.Mapper());
    }

    public static IEnumerable<TipoSpedizioneResponse> Mapper(this IEnumerable<TipoSpedizione> models)
    {
        return models.Select(x => x.Mapper());
    }

    public static TipoSpedizioneResponse Mapper(this TipoSpedizione model) =>
       new()
       {
           Descrizione = model.Descrizione,
           Id = model.Id,
           Tipo = model.Tipo
       };

    public static IEnumerable<CategoriaSpedizioneResponse> Mapper(this IEnumerable<CategoriaSpedizione> models)
    {
        return models.Select(x => x.Mapper());
    }

    public static CategoriaSpedizioneResponse Mapper(this CategoriaSpedizione model) =>
       new()
       {
           Descrizione = model.Descrizione,
           Id = model.Id,
           Tipo = model.Tipo,
           TipoSpedizione = model.TipoSpedizione!.Mapper().ToList()
       };
}