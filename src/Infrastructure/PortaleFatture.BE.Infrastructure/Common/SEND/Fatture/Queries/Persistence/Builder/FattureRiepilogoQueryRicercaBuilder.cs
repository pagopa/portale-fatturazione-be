using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

public static class FattureRiepilogoQueryRicercaBuilder
{
    private static string _sqlSelectAllAnni = 
        @"
          SELECT AnnoRiferimento as Anno
          FROM [pfd].[RiepilogoFatturazione_NPF] npf
          GROUP BY AnnoRiferimento
        ";

    public static string SelectAllAnni()
    {
        return _sqlSelectAllAnni;
    }

    private static string _sqlSelectAllMesi = 
        @"  
          SELECT MeseRiferimento as Mese
          FROM [pfd].[RiepilogoFatturazione_NPF] npf
          GROUP BY MeseRiferimento, AnnoRiferimento
        ";

    public static string SelectAllMesi()
    {
        return _sqlSelectAllMesi;
    }

    public static string OrderByYear()
    {
        return " ORDER BY AnnoRiferimento desc";
    }
    public static string OrderByMonth()
    {
        return " ORDER BY MeseRiferimento desc";
    }
}
