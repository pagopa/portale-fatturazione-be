namespace PortaleFatture.BE.Core.Auth.PagoPA;

public class GroupRoles
{
    public const string APPROVIGIONAMENTO = "procurement";
    public const string FINANZA = "finance";
    public const string ASSISTENZA = "assistenza";
    public const string PORTALE = "portalefatturazione-app";

    public static Dictionary<string, bool> GetRoles(string prefix)
    {
        return new Dictionary<string, bool>()
        {
            { $"{prefix}{APPROVIGIONAMENTO}", false },
            { $"{prefix}{FINANZA}", false },
            { $"{prefix}{ASSISTENZA}", false },
            { $"{prefix}{PORTALE}", true }
        };
    }
}