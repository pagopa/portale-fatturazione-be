using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortaleFatture.BE.Core.Auth;

public static class Ruolo
{
    public const string MANAGER = ADMIN;
    public const string DELEGATE = ADMIN;
    public const string SUB_DELEGATE = ADMIN;
    public const string ADMIN = "R/W";
    public const string OPERATOR = "R";
}