using System.Net.Mail;

namespace PortaleFatture.BE.Core.Extensions;

/// <summary>
/// Fornisce metodi di estensione per la validazione di sequenze, stringhe e formati comuni come indirizzi e-mail e
/// GUID.
/// </summary>
/// <remarks>Questa classe contiene metodi di utilità per semplificare le verifiche di nullità, vuotezza e
/// validità di stringhe e raccolte. I metodi sono progettati per essere utilizzati come estensioni su tipi standard
/// .NET, migliorando la leggibilità e la concisione del codice di validazione.</remarks>
public static class ValidationExtensions
{ 
    /// <summary>
    /// Determina se la sequenza specificata è null o non contiene elementi.
    /// </summary>
    /// <typeparam name="T">Il tipo degli elementi nella sequenza.</typeparam>
    /// <param name="enumerable">La sequenza di elementi da verificare. Può essere null.</param>
    /// <returns>true se la sequenza è null o non contiene elementi; in caso contrario, false.</returns>
    public static bool IsNullNotAny<T>(this IEnumerable<T>? enumerable)
    {
        return enumerable == null || !enumerable.Any();
    }

    //public static bool IsNull<T>(this T? obj) where T : class
    //{
    //    return obj == null;
    //}


    /// <summary>
    /// Determina se la stringa specificata è null, vuota o composta solo da spazi bianchi.
    /// </summary>
    /// <remarks>Questo metodo considera come null anche le stringhe che contengono solo caratteri di spazio
    /// bianco, a differenza di un semplice controllo di nullità.</remarks>
    /// <param name="value">La stringa da verificare per nullità o presenza esclusiva di spazi bianchi.</param>
    /// <returns>true se la stringa è null, vuota o contiene solo caratteri di spazio bianco; altrimenti, false.</returns>
    public static bool IsNull(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Determina se la stringa specificata non rappresenta un indirizzo e-mail valido.
    /// </summary>
    /// <remarks>La validazione considera una stringa nulla o vuota come non valida. La verifica si basa sul
    /// formato standard degli indirizzi e-mail.</remarks>
    /// <param name="value">La stringa da verificare come indirizzo e-mail.</param>
    /// <returns>Restituisce <see langword="true"/> se la stringa non è un indirizzo e-mail valido; in caso contrario, <see
    /// langword="false"/>.</returns>
    public static bool IsNotValidEmail(this string value)
    {
        if (value.IsNull()) return true;

        try
        {
            var email = new MailAddress(value);
            return email.Address != value.Trim();
        }
        catch
        {
            return true;
        }
    }
    
    /// <summary>
    /// Determina se la stringa specificata non rappresenta un identificatore GUID valido.
    /// </summary>
    /// <param name="value">La stringa da verificare come GUID. Può essere null o vuota.</param>
    /// <returns>true se la stringa non è un GUID valido; in caso contrario, false.</returns>
    public static bool IsNotValidGuid(this string value)
    {
        return !Guid.TryParse(value, out _);
    }
}
