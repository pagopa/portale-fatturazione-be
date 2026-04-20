using System.Data;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Services;

public class EmailRelService(string cn) : IEmailRelService
{
    private readonly string _cn = cn;
    private readonly string _sqlSelect = @"
        SELECT  
         [internal_organization_id] as idEnte
        ,[contract_id] as idContratto
        ,[TipologiaFattura]
        ,er.[year] as anno
        ,er.[month] as mese
        ,[PEC] as pec
        ,[RagioneSociale] as ragionesociale
        ,[FlagConguaglio] as semestre
        ,tp.IdTipoContratto
        ,tp.Descrizione as TipoContratto
        FROM [pfd].[EmailRel] er
        LEFT JOIN pfd.Contratti c
        ON c.internalistitutionid = er.[internal_organization_id]
        INNER JOIN pfw.TipoContratto tp
        ON c.FkIdTipoContratto = tp.IdTipoContratto
        WHERE tp.Descrizione = 'PAC' AND er.[year] = @year
        AND er.[month] = @month
        AND [TipologiaFattura] = @tipologiaFattura
        AND Totale >0;";

    private readonly string _sqlInsert = @"
    INSERT INTO [pfd].[RelEmail]
            ([FkIdEnte]
            ,[contract_id]
            ,[TipologiaFattura]
            ,[year]
            ,[month]
            ,[DataEvento]
            ,[Pec]
            ,[Messaggio]
            ,[RagioneSociale]
            ,[TipoComunicazione]
            ,[Invio]
            ,[Fase])
        VALUES
            (@idEnte
            ,@idContratto
            ,@TipologiaFattura
            ,@anno
            ,@mese
            ,@data
            ,@pec
            ,@messaggio
            ,@RagioneSociale
            ,@TipoComunicazione
            ,@Invio
            ,@Fase);";

    private readonly string _sqlSelectFatture = @"
with cte_emesse as (
 
select 
     IdFattura
    , FkIdEnte
    , CodiceContratto
    , FkTipologiaFattura
    , AnnoRiferimento
    , MeseRiferimento
FROM [pfd].[FattureTestata] ft
        WHERE ft.AnnoRiferimento = @year
        AND ft.MeseRiferimento = @month
        AND ft.TotaleFattura > 0
        AND ft.FkTipologiaFattura IN ('PRIMO SALDO', 'SECONDO SALDO','VAR. SEMESTRALE')
)
, cte_mesifatture as (
    select 
        mf.FkIdFattura
        , mf.FkIdFatturaTmp
        , ft.FkIdEnte
        , ft.CodiceContratto
        , ft.FkTipologiaFattura
        , ft.AnnoRiferimento
        , ft.MeseRiferimento
        , count(FkIdFattura) over (partition by FkIdFattura) as NumeroRighe
        , tft.annoriferimento as tmpAnno
        , tft.MeseRiferimento as tmpMese
        , tft.FlagFatturata
    from pfd.mesifatture mf
        inner join cte_emesse ft 
            on ft.IdFattura = mf.FkIdFattura
        inner join pfd.tmpFattureTestata tft
            on tft.IdFattura = mf.FkIdFatturaTmp
),
 
cte_sospese as (
select 
      ft.IdFattura
    , NULL as FkIdFatturaTmp
    , ft.FkIdEnte
    , ft.CodiceContratto
    , ft.FkTipologiaFattura
    , ft.AnnoRiferimento
    , ft.MeseRiferimento
    , count(ft.IdFattura) over (partition by ft.IdFattura) as NumeroRighe
    , ft.AnnoRiferimento as tmpAnno
    , ft.MeseRiferimento as tmpMese
    , ft.FlagFatturata as FlagFatturata
    ,'sospese' as StatoFattura
FROM [pfd].[tmpFattureTestata] ft
left join pfd.mesifatture mf
    on mf.FkIdFatturaTmp = ft.IdFattura
        WHERE ft.AnnoRiferimento = @year
        AND ft.MeseRiferimento = @month
        AND ft.TotaleFattura > 0
        AND ft.FlagFatturata = 0
        AND mf.FkIdFatturatmp is null
        AND ft.FkTipologiaFattura IN ('PRIMO SALDO', 'SECONDO SALDO','VAR. SEMESTRALE')
)
 
select 
    mf.FkIdEnte as idEnte
    , mf.CodiceContratto as idContratto
    , mf.FkTipologiaFattura as TipologiaFattura
    , mf.AnnoRiferimento as anno
    , mf.MeseRiferimento as mese
    , e.digitalAddress as pec
    , e.description as ragionesociale
    , tc.Descrizione as TipoContratto
    , mf.NumeroRighe
    , mf.tmpAnno
    , mf.tmpMese
    , mf.FlagFatturata
    , 'emesse' as StatoFattura
from cte_mesifatture mf
    INNER JOIN [pfd].[Enti] e ON e.InternalIstitutionId = mf.FkIdEnte
    INNER JOIN [pfd].[Contratti] c ON c.internalistitutionid = mf.FkIdEnte AND c.onboardingtokenid = mf.CodiceContratto
    INNER JOIN [pfw].[TipoContratto] tc ON tc.IdTipoContratto = c.FkIdTipoContratto
    WHERE FkTipologiaFattura = @tipologiaFattura

union all

select 
    cs.FkIdEnte as idEnte
    , cs.CodiceContratto as idContratto
    , cs.FkTipologiaFattura as TipologiaFattura
    , cs.AnnoRiferimento as anno
    , cs.MeseRiferimento as mese
    , e.digitalAddress as pec
    , e.description as ragionesociale
    , tc.Descrizione as TipoContratto
    , cs.NumeroRighe
    , cs.tmpAnno
    , cs.tmpMese
    , cs.FlagFatturata
    , cs.StatoFattura
from cte_sospese cs
    INNER JOIN [pfd].[Enti] e ON e.InternalIstitutionId = cs.FkIdEnte
    INNER JOIN [pfd].[Contratti] c ON c.internalistitutionid = cs.FkIdEnte AND c.onboardingtokenid = cs.CodiceContratto
    INNER JOIN [pfw].[TipoContratto] tc ON tc.IdTipoContratto = c.FkIdTipoContratto
    WHERE FkTipologiaFattura = @tipologiaFattura
 ";

    public IEnumerable<RelEmail>? GetSenderEmail(int? anno, int? mese, string tipologiaFattura, string? tipoComunicazione)
    {
        if (string.IsNullOrEmpty(tipoComunicazione))
            throw new ArgumentException("Tipo Comunicazione obbligatorio");

        List<RelEmail> emails = [];
        try
        {
            using var conn = new SqlConnection(_cn);
            conn.Open();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.Add("@year", SqlDbType.Int).Value = anno;
            cmd.Parameters.Add("@month", SqlDbType.Int).Value = mese;
            if(tipoComunicazione == "REL")
            {
                cmd.Parameters.Add("@tipologiaFattura", SqlDbType.NVarChar).Value = tipologiaFattura;
                cmd.CommandText = _sqlSelect;
                var reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        emails.Add(new RelEmail()
                        {
                            IdEnte = reader.GetString(0),
                            IdContratto = reader.GetString(1),
                            TipologiaFattura = reader.GetString(2),
                            Anno = reader.GetInt32(3),
                            Mese = reader.GetInt32(4),
                            Pec = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            RagioneSociale = reader.GetString(6),
                            Semestre = reader.IsDBNull(7) ? null : reader.GetString(7),
                            TipoContratto = reader.GetString(9)
                        });
                    }
                }
                reader.Close();
            }
            else if(tipoComunicazione == "FATTURA")
            {
                cmd.Parameters.Add("@tipologiaFattura", SqlDbType.NVarChar).Value = tipologiaFattura;
                cmd.CommandText = _sqlSelectFatture;
                var reader = cmd.ExecuteReader();
                var rawEmails = new List<dynamic>();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        rawEmails.Add(new 
                        {
                            IdEnte = reader.GetString(0),
                            IdContratto = reader.GetString(1),
                            TipologiaFattura = reader.GetString(2),
                            Anno = reader.GetInt32(3),
                            Mese = reader.GetInt32(4),
                            Pec = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            RagioneSociale = reader.GetString(6),
                            TipoContratto = reader.GetString(7),
                            NumeroRighe = reader.GetInt32(8),
                            TmpAnno = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                            TmpMese = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10),
                            FlagFatturata = reader.GetBoolean(11)
                        });
                    }
                }
                reader.Close();

                var grouped = rawEmails.GroupBy(x => new { x.IdEnte, x.IdContratto, x.TipologiaFattura, x.Anno, x.Mese });
                foreach(var g in grouped)
                {
                    var first = g.First();
                    var elencoMesi = string.Join(", ", g.Where(x => x.TmpMese != null && x.TmpAnno != null)
                        .Select(x => new { Anno = (int)x.TmpAnno, Mese = (int)x.TmpMese })
                        .Distinct()
                        .OrderByDescending(x => x.Anno)
                        .ThenByDescending(x => x.Mese)
                        .Select(x => $"{x.Mese:00}/{x.Anno}"));
                    emails.Add(new RelEmail()
                    {
                        IdEnte = first.IdEnte,
                        IdContratto = first.IdContratto,
                        TipologiaFattura = first.TipologiaFattura,
                        Anno = first.Anno,
                        Mese = first.Mese,
                        Pec = first.Pec,
                        RagioneSociale = first.RagioneSociale,
                        TipoContratto = first.TipoContratto,
                        NumeroRighe = first.NumeroRighe,
                        FlagFatturata = first.FlagFatturata,
                        ElencoMesi = elencoMesi
                    });
                }
            }
        }
        catch(Exception ex)
        {
            throw new Exception($"Error in EmailRelService: {ex.Message}", ex);
        }
        return emails;
    }

    public bool InsertTracciatoEmail(RelEmailTracking email)
    {
        try
        {
            using var conn = new SqlConnection(_cn);
            conn.Open();
            using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.Add("@idEnte", SqlDbType.NVarChar).Value = email.IdEnte;
            cmd.Parameters.Add("@idContratto", SqlDbType.NVarChar).Value = email.IdContratto;
            cmd.Parameters.Add("@TipologiaFattura", SqlDbType.NVarChar).Value = email.TipologiaFattura;
            cmd.Parameters.Add("@anno", SqlDbType.Int).Value = email.Anno;
            cmd.Parameters.Add("@mese", SqlDbType.Int).Value = email.Mese;
            cmd.Parameters.Add("@data", SqlDbType.NVarChar).Value = email.Data;
            cmd.Parameters.Add("@pec", SqlDbType.NVarChar).Value = email.Pec;
            cmd.Parameters.Add("@messaggio", SqlDbType.NVarChar).Value = email.Messaggio;
            cmd.Parameters.Add("@RagioneSociale", SqlDbType.NVarChar).Value = email.RagioneSociale;
            cmd.Parameters.Add("@TipoComunicazione", SqlDbType.NVarChar).Value = email.TipoComunicazione;
            cmd.Parameters.Add("@Invio", SqlDbType.Bit).Value = email.Invio;
            cmd.Parameters.Add("@Fase", SqlDbType.Int).Value = email.Fase;
            cmd.CommandText = _sqlInsert;
            var rows = cmd.ExecuteNonQuery();
            return rows == 1;
        }
        catch
        {

            return false;
        }
    }
}