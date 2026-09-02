using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Unit test (senza DB) sulla logica pura delle Query di GestioneFatture: la normalizzazione del
/// setter IdEnti, che azzera gli array vuoti a null cosi' il filtro non viene aggiunto nella persistence.
/// </summary>
public class GestioneFattureQueryTests
{
    private static AuthenticationInfo Auth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    [Test]
    public void IdEnti_SetEmptyArray_ShouldBecomeNull()
    {
        var q = new GestioneFattureQuery(Auth()) { IdEnti = Array.Empty<string>() };
        Assert.That(q.IdEnti, Is.Null, "Un array vuoto deve essere normalizzato a null (nessun filtro).");
    }

    [Test]
    public void IdEnti_SetNull_ShouldStayNull()
    {
        var q = new GestioneFattureQuery(Auth()) { IdEnti = null };
        Assert.That(q.IdEnti, Is.Null);
    }

    [Test]
    public void IdEnti_SetNonEmpty_ShouldBePreserved()
    {
        var enti = new[] { "ente-1", "ente-2" };
        var q = new GestioneFattureQuery(Auth()) { IdEnti = enti };
        Assert.That(q.IdEnti, Is.EqualTo(enti));
    }

    [Test]
    public void DownloadQuery_IdEnti_SetEmptyArray_ShouldBecomeNull()
    {
        var q = new GestioneFattureDownloadQuery(Auth()) { IdEnti = Array.Empty<string>() };
        Assert.That(q.IdEnti, Is.Null);
    }

    [Test]
    public void DownloadQuery_IdEnti_SetNonEmpty_ShouldBePreserved()
    {
        var enti = new[] { "ente-1" };
        var q = new GestioneFattureDownloadQuery(Auth()) { IdEnti = enti };
        Assert.That(q.IdEnti, Is.EqualTo(enti));
    }
}
