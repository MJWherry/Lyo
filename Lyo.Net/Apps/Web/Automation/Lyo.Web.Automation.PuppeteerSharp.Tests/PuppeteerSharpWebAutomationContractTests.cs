using Lyo.Web.Automation.Tests;
using Lyo.Web.Automation.Tests.Fixtures;

namespace Lyo.Web.Automation.PuppeteerSharp.Tests;

public sealed class PuppeteerSharpWebAutomationContractTests
    : WebAutomationContractTests<PuppeteerSharpWebAutomationTestEngineFixture>, IClassFixture<PuppeteerSharpWebAutomationTestEngineFixture>,
        IClassFixture<WebAutomationTestPageHostFixture>
{
    public PuppeteerSharpWebAutomationContractTests(PuppeteerSharpWebAutomationTestEngineFixture factory, WebAutomationTestPageHostFixture pageHost, ITestOutputHelper output)
        : base(factory, pageHost)
        => factory.UseTestOutputLogger(output);
}