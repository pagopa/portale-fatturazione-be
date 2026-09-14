using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Extensions;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// `ApiKeysExtensions.VerifyIp`: la validazione che filtra cosa può entrare nella whitelist IP delle
/// Integration API. È l'unico pezzo di logica pura dell'area ApiKeys, ed è sicurezza: un indirizzo
/// accettato per errore diventa un IP autorizzato a chiamare le API di un aderente.
///
/// Due rami distinti nell'implementazione: con '/' si tenta `IPNetwork.Parse` (CIDR), senza '/' si
/// usa `IPAddress.TryParse`. Entrambi con catch-all che degrada a false.
/// </summary>
public class ApiKeysVerifyIpTests
{
    [TestCase(null, TestName = "VerifyIp · null")]
    [TestCase("", TestName = "VerifyIp · stringa vuota")]
    [TestCase("   ", TestName = "VerifyIp · solo spazi")]
    public void VerifyIp_ValoriVuoti_ShouldReturnFalse(string? valore)
        => Assert.That(valore.VerifyIp(), Is.False);

    [TestCase("192.168.1.1")]
    [TestCase("10.0.0.255")]
    [TestCase("::1")]
    [TestCase("2001:db8::1")]
    public void VerifyIp_IndirizziSingoliValidi_ShouldReturnTrue(string indirizzo)
        => Assert.That(indirizzo.VerifyIp(), Is.True);

    [TestCase("999.1.1.1", TestName = "VerifyIp · ottetto fuori range")]
    [TestCase("non-un-ip", TestName = "VerifyIp · testo")]
    public void VerifyIp_IndirizziSingoliNonValidi_ShouldReturnFalse(string indirizzo)
        => Assert.That(indirizzo.VerifyIp(), Is.False);

    /// <summary>
    /// ATTENZIONE Trappola di sicurezza, non un difetto della nostra validazione: `IPAddress.TryParse` accetta
    /// le forme dotted-quad ABBREVIATE e le espande secondo la convenzione storica di inet_aton.
    /// "192.168.1" NON viene rifiutato: diventa 192.168.0.1, cioè un indirizzo DIVERSO da quello che
    /// l'utente aveva in mente. In una whitelist IP significa autorizzare un host non voluto senza
    /// alcun messaggio d'errore.
    ///
    /// Il test caratterizza il comportamento reale (verificato il 06/08/2026) e lascia traccia del
    /// rischio. Se si volesse renderlo stretto, il punto di intervento è `VerifyIp`, non qui — e
    /// andrebbe coordinato col frontend, che oggi accetta lo stesso input.
    /// </summary>
    [TestCase("192.168.1", "192.168.0.1", TestName = "VerifyIp · forma abbreviata a 3 ottetti")]
    [TestCase("10.1", "10.0.0.1", TestName = "VerifyIp · forma abbreviata a 2 ottetti")]
    public void VerifyIp_FormeAbbreviate_VengonoAccettateEEspanse_Caratterizzazione(
        string scritto, string interpretatoAtteso)
    {
        Assert.That(scritto.VerifyIp(), Is.True,
            "Comportamento attuale: la forma abbreviata passa la validazione.");

        Assert.That(System.Net.IPAddress.Parse(scritto).ToString(), Is.EqualTo(interpretatoAtteso),
            $"'{scritto}' viene interpretato come '{interpretatoAtteso}': in whitelist finisce un "
            + "indirizzo diverso da quello digitato.");
    }

    [TestCase("192.168.1.0/24")]
    [TestCase("10.0.0.0/8")]
    [TestCase("2001:db8::/32")]
    public void VerifyIp_CidrValidi_ShouldReturnTrue(string cidr)
        => Assert.That(cidr.VerifyIp(), Is.True);

    [TestCase("192.168.1.0/33", TestName = "VerifyIp · prefisso oltre 32 su IPv4")]
    [TestCase("192.168.1.0/abc", TestName = "VerifyIp · prefisso non numerico")]
    [TestCase("192.168.1.0/", TestName = "VerifyIp · prefisso assente")]
    [TestCase("/24", TestName = "VerifyIp · indirizzo assente")]
    public void VerifyIp_CidrNonValidi_ShouldReturnFalse(string cidr)
        => Assert.That(cidr.VerifyIp(), Is.False);

    /// <summary>
    /// Caso che conta per chi compila la whitelist dal portale: un CIDR con i bit host valorizzati
    /// (192.168.1.5/24 invece di 192.168.1.0/24) è l'errore di digitazione più comune. Il test fissa
    /// il comportamento reale di `System.Net.IPNetwork.Parse` su .NET 8, così una futura modifica
    /// dell'implementazione non lo cambia in silenzio: l'utente deve continuare a ricevere lo stesso
    /// esito, qualunque esso sia.
    /// </summary>
    [Test]
    public void VerifyIp_CidrConBitHostValorizzati_Caratterizzazione()
    {
        var esito = "192.168.1.5/24".VerifyIp();

        TestContext.Out.WriteLine($"192.168.1.5/24 -> {esito}");
        Assert.That(esito, Is.False,
            "IPNetwork.Parse su .NET 8 rifiuta un CIDR con bit host non azzerati. Se questo test "
            + "diventa rosso l'implementazione è cambiata: verificare che il portale continui a dare "
            + "un messaggio comprensibile a chi digita 192.168.1.5/24.");
    }
}
