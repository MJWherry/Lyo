using Lyo.Web.Automation.Tests;
using Lyo.Web.Automation.Tests.Fixtures;
using Xunit;

namespace Lyo.Web.Automation.Selenium.Tests;

public sealed class SeleniumWebAutomationContractTests
    : WebAutomationContractTests<SeleniumWebAutomationTestEngineFixture>,
        IClassFixture<SeleniumWebAutomationTestEngineFixture>,
        IClassFixture<WebAutomationTestPageHostFixture>
{
    public SeleniumWebAutomationContractTests(
        SeleniumWebAutomationTestEngineFixture factory,
        WebAutomationTestPageHostFixture pageHost,
        ITestOutputHelper output)
        : base(factory, pageHost)
    {
        factory.UseTestOutputLogger(output);
    }
}
