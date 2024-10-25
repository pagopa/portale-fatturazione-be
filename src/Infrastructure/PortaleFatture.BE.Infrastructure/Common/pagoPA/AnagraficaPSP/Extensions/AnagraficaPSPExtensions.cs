namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Extensions;

public static class AnagraficaPSPExtensions
{
    public static void AddInOrder(this List<string> list, string item)
    { 
        var index = list.BinarySearch(item); 
        if (index < 0) 
            index = ~index;    
        list.Insert(index, item);
    }
} 