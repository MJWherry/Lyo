namespace Lyo.Scraping;

public class ScraperOptions
{
    public List<string> UserAgents { get; set; } = [
        "Mozilla/5.0 (X11; Windows; Windows x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.246",
        "Mozilla/5.0 (X11; CrOS x86_64 8172.45.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.64 Safari/537.36",
        "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.111 Safari/537.36"
    ];

    /// <summary> Default HTTP headers to include in requests. </summary>
    public Dictionary<string, string> DefaultHeaders { get; set; } = new() {
        { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8" },
        { "Accept-Encoding", "gzip, deflate, br, zstd" },
        { "Accept-Language", "en-US,en;q=0.5" },
        { "Connection", "keep-alive" },
        { "Upgrade-Insecure-Requests", "1" },
        { "Sec-Fetch-Dest", "document" },
        { "Sec-Fetch-Mode", "navigate" },
        { "Sec-Fetch-Site", "same-origin" },
        { "Sec-Fetch-User", "?1" },
        { "Priority", "u=0, i" }
    };

    /// <summary> Chrome WebDriver arguments. </summary>
    public List<string> WebDriverArguments { get; set; } = ["disable-infobars", "disable-extensions", "disable-gpu", "disable-dev-shm-usage", "no-sandbox", "headless"];

    /// <summary> Browser window size (width, height). </summary>
    public int BrowserWindowWidth { get; set; } = 1280;

    public int BrowserWindowHeight { get; set; } = 1024;

    /// <summary> Page load timeout in seconds. </summary>
    public int PageLoadTimeoutSeconds { get; set; } = 30;

    /// <summary> Implicit wait timeout in seconds. </summary>
    public int ImplicitWaitSeconds { get; set; } = 10;

    /// <summary> Script timeout in seconds. </summary>
    public int ScriptTimeoutSeconds { get; set; } = 30;

    /// <summary> Maximum wait time for Selenium operations in seconds. </summary>
    public int SeleniumMaxWaitSeconds { get; set; } = 15;

    /// <summary> Whether to record metrics for scraping operations. </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary> Resilience pipeline name for polling operations. Uses default when null. </summary>
    public string? PollingPipelineName { get; set; }
}