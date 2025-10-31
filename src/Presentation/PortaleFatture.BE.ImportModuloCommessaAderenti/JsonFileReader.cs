using System.Text.Json;

namespace PortaleFatture.BE.ImportModuloCommessaAderenti;

public class JsonFileReader
{
    public static List<MooduloCommessaAderentiDto> ProcessJsonFile(string filePath)
    {
        try
        {
            var jsonArrayString = File.ReadAllText(filePath);
            Console.WriteLine($"Contenuto del file letto con successo da: {filePath}");

            var dataList = JsonSerializer.Deserialize<List<MooduloCommessaAderentiDto>>(jsonArrayString);
            if (dataList == null)
            {
                Console.WriteLine("Errore: Il file JSON non contiene dati validi.");
                return []; 
            }

            Console.WriteLine($"Deserializzati {dataList.Count} oggetti dall'array JSON.");
            return dataList;
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Errore: Il file '{filePath}' non è stato trovato.");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Errore di deserializzazione JSON: {ex.Message}");
            Console.WriteLine($"Dettagli: {ex.StackTrace}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Si è verificato un errore inatteso: {ex.Message}");
            Console.WriteLine($"Dettagli: {ex.StackTrace}");
        } 
 
        return [];
    }
}