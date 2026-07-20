using Microsoft.DurableTask;
using Moq;
using PortaleFatture.BE.Core.Entities.pagoPA.AnagraficaPSP;
using PortaleFatture_BE_SendEmailFunction;
using PortaleFatture_BE_SendEmailFunction.Models.pagoPA;
using PortaleFatture_BE_SendEmailFunction.Orchestrators;

namespace PortaleFatture.BE.UnitTest;

public class SendEmailPspAdjustmentOrchestratorTests
{
    [Test]
    public async Task RunAsync_ShouldReturnActivityResponse()
    {
        // Arrange
        var input = new EmailPspAdjustmentDataRequest { Preview = true };
        var expected = new RispostapagoPA
        {
            Anno = 2026,
            Trimestre = "2026_1",
            Tipologia = EmailPspTipologia.FinancialAdjust,
            Processati = 10,
            LogAnteprime = 10
        };

        var context = new Mock<TaskOrchestrationContext>(MockBehavior.Strict);

        context
            .Setup(x => x.GetInput<EmailPspAdjustmentDataRequest>())
            .Returns(input);

        context
            .Setup(x => x.CallActivityAsync<RispostapagoPA>(
                nameof(SendEmailPspAdjustment),
                input,
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(expected);

        var sut = new SendEmailPspAdjustmentOrchestrator();

        // Act
        var result = await sut.RunAsync(context.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Anno, Is.EqualTo(expected.Anno));
        Assert.That(result.Trimestre, Is.EqualTo(expected.Trimestre));
        Assert.That(result.Tipologia, Is.EqualTo(expected.Tipologia));
        Assert.That(result.Processati, Is.EqualTo(expected.Processati));
        Assert.That(result.LogAnteprime, Is.EqualTo(expected.LogAnteprime));

        context.VerifyAll();
    }

    [Test]
    public async Task RunAsync_ShouldForwardInputToActivity_WhenPreviewIsFalse()
    {
        // Arrange
        var input = new EmailPspAdjustmentDataRequest { Preview = false };
        var expected = new RispostapagoPA
        {
            Anno = 2026,
            Trimestre = "2026_1",
            Tipologia = EmailPspTipologia.FinancialAdjust,
            Processati = 1,
            Loggati = 1
        };

        var context = new Mock<TaskOrchestrationContext>(MockBehavior.Strict);

        context
            .Setup(x => x.GetInput<EmailPspAdjustmentDataRequest>())
            .Returns(input);

        context
            .Setup(x => x.CallActivityAsync<RispostapagoPA>(
                nameof(SendEmailPspAdjustment),
                It.Is<EmailPspAdjustmentDataRequest>(r => r.Preview == false),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(expected);

        var sut = new SendEmailPspAdjustmentOrchestrator();

        // Act
        var result = await sut.RunAsync(context.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Loggati, Is.EqualTo(1));

        context.VerifyAll();
    }

    [Test]
    public void SendEmailPspAdjustment_RunAsync_ShouldReturnTaskOfRispostaPagoPA()
    {
        // Arrange
        var method = typeof(SendEmailPspAdjustment).GetMethod(nameof(SendEmailPspAdjustment.RunAsync));

        // Assert
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<RispostapagoPA>)));
    }
}
