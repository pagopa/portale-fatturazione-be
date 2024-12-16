using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence.Builder;

internal sealed class RelNonFatturateSQLBuilder
{
    private static string _sql = @"
SELECT e.description              AS RagioneSociale,
       r.internal_organization_id AS IdEnte,
       t.descrizione              AS TipoContratto,
       e.category                 AS Category,
       r.contract_id              AS IdContratto,
       r.year                     AS anno,
       r.month                    AS mese,
       r.tipologiafattura,
       r.totale,
       r.totaleiva,
       r.caricata,
       u.dataevento               AS data
FROM   pfd.reltestata r
       LEFT JOIN pfd.relupload u
              ON r.internal_organization_id = u.fkidente
                 AND r.contract_id = u.contract_id
                 AND r.year = u.year
                 AND r.month = u.month
                 AND r.tipologiafattura = u.tipologiafattura
       LEFT OUTER JOIN pfd.enti e
                    ON e.internalistitutionid = r.internal_organization_id
       LEFT OUTER JOIN pfd.contratti c
                    ON e.internalistitutionid = c.internalistitutionid
       LEFT OUTER JOIN pfw.tipocontratto t
                    ON t.idtipocontratto = c.fkidtipocontratto
WHERE  r.relfatturata = 0
       AND totale > 0
ORDER  BY r.year DESC,
          r.month DESC,
          r.tipologiafattura 
    ";

    public static string SelectAll()
    {
        return _sql;
    }
}
