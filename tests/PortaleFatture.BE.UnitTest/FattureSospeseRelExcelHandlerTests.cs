using System.Data;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;

namespace PortaleFatture.BE.UnitTest;

public class FattureSospeseRelExcelHandlerTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Handle_QueriesOnlyRel_AndPreservesThreeSlots(bool empty)
    {
        using var cancellation = new CancellationTokenSource();
        var ct = cancellation.Token;
        var rel = empty ? Array.Empty<FattureRelSospeseExcelDto>() : new[] { new FattureRelSospeseExcelDto { IdFattura = "1" } };
        var report = empty ? Array.Empty<FattureRelSospeseExcelDto>() : new[] { new FattureRelSospeseExcelDto { IdFattura = "2" } };
        var context = new Mock<IDbContext>(MockBehavior.Strict);
        context.Setup(x => x.Query(It.IsAny<FattureSospeseRelExcelBuilderPersistence>(), ct))
            .ReturnsAsync(rel);
        context.Setup(x => x.Query(It.IsAny<FattureSospeseUnionRelExcelPersistence>(), ct))
            .ReturnsAsync(report);
        context.Setup(x => x.Dispose());
        var factory = new Mock<ISelfCareDbContextFactory>(MockBehavior.Strict);
        factory.Setup(x => x.Create(false, IsolationLevel.ReadCommitted, ct)).ReturnsAsync(context.Object);

        var result = await Handler(factory.Object).Handle(new FattureSospeseRelExcelQuery(new AuthenticationInfo()), ct);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result![0], Is.SameAs(rel));
            Assert.That(result[1], Is.SameAs(report));
            Assert.That(result[2], Is.Not.Null.And.Empty);
        });
        factory.Verify(x => x.Create(false, IsolationLevel.ReadCommitted, ct), Times.Exactly(2));
        context.Verify(x => x.Query(It.IsAny<FattureSospeseRelExcelBuilderPersistence>(), ct), Times.Once);
        context.Verify(x => x.Query(It.IsAny<FattureSospeseUnionRelExcelPersistence>(), ct), Times.Once);
        context.Verify(x => x.Dispose(), Times.Exactly(2));
        factory.VerifyNoOtherCalls();
        context.VerifyNoOtherCalls();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Handle_RelQueryFailure_IsNotConvertedToEmptyReport(bool reportFails)
    {
        var failure = new DataException("Database unavailable");
        var context = new Mock<IDbContext>(MockBehavior.Strict);
        context.Setup(x => x.Query(It.IsAny<FattureSospeseRelExcelBuilderPersistence>(), CancellationToken.None))
            .ReturnsAsync(Array.Empty<FattureRelSospeseExcelDto>());
        if (reportFails)
            context.Setup(x => x.Query(It.IsAny<FattureSospeseUnionRelExcelPersistence>(), CancellationToken.None))
                .ThrowsAsync(failure);
        else
            context.Setup(x => x.Query(It.IsAny<FattureSospeseRelExcelBuilderPersistence>(), CancellationToken.None))
                .ThrowsAsync(failure);
        context.Setup(x => x.Dispose());
        var factory = new Mock<ISelfCareDbContextFactory>(MockBehavior.Strict);
        factory.Setup(x => x.Create(false, IsolationLevel.ReadCommitted, CancellationToken.None)).ReturnsAsync(context.Object);

        var thrown = Assert.ThrowsAsync<DataException>(() =>
            Handler(factory.Object).Handle(new FattureSospeseRelExcelQuery(new AuthenticationInfo()), CancellationToken.None));

        Assert.That(thrown, Is.SameAs(failure));
        context.Verify(x => x.Dispose(), Times.Exactly(reportFails ? 2 : 1));
    }

    private static FattureSospeseRelExcelHandler Handler(ISelfCareDbContextFactory factory) =>
        new(factory, Mock.Of<IStringLocalizer<Localization>>(), NullLogger<FattureSospeseRelExcelHandler>.Instance);
}
