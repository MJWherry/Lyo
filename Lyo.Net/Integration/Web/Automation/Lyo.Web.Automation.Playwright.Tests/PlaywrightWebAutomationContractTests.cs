using Lyo.Web.Automation.Tests;
using Lyo.Web.Automation.Tests.Fixtures;
using Xunit;

namespace Lyo.Web.Automation.Playwright.Tests;

public sealed class PlaywrightWebAutomationContractTests
    : WebAutomationContractTests<PlaywrightWebAutomationTestEngineFixture>,
        IClassFixture<PlaywrightWebAutomationTestEngineFixture>,
        IClassFixture<WebAutomationTestPageHostFixture>
{
    public PlaywrightWebAutomationContractTests(
        PlaywrightWebAutomationTestEngineFixture factory,
        WebAutomationTestPageHostFixture pageHost,
        ITestOutputHelper output)
        : base(factory, pageHost)
    {
        factory.UseTestOutputLogger(output);
    }
}
