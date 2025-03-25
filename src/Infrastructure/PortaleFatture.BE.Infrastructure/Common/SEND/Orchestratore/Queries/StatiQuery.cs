namespace PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries;

public static class StatiQuery
{ 
    public static Dictionary<int, string> GetStati()
    {
        return new Dictionary<int, string>()
        {
            { 0 , "Programmato"},
            { 1 , "Eseguito"},
            { 2 , "Eseguito no data"},
            { 3 , "Errore"}
        };
    }
}
