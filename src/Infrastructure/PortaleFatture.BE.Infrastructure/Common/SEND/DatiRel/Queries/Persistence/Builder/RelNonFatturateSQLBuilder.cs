using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence.Builder;

internal sealed class RelNonFatturateSQLBuilder
{
    private static string _sql = @"
    SELECT * FROM [be].[vwDownloadRelPAC]";

    public static string SelectAll()
    {
        return _sql;
    }
}
