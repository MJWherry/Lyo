using System.Text.Json;
using Lyo.Exceptions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;
using Lyo.Web.Automation.Plan;
using Lyo.Web.Automation.Playwright;
using Lyo.Web.Automation.Playwright.Browser;
using Lyo.Web.Automation.Playwright.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Lyo.Web.Automation.Playwright.Record;

/// <summary>Opens a Playwright browser for the automation studio. Record, play, and tools live on <see cref="IPlaywrightAutomationStudio" />.</summary>
public sealed class PlaywrightAutomationStudio(IPlaywrightBrowserService browsers, IAutomationPlanRunner? runner = null)
{
    private const string HighlightSource = """
        el => {
          if (window.__lyoTool && window.__lyoTool.mark)
            window.__lyoTool.mark(el);
        }
        """;

    private static readonly JsonSerializerOptions MessageOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Starts the browser and installs capture. The page stays the site.</summary>
    public async Task<IPlaywrightAutomationStudio> StartAsync(PlaywrightAutomationStudioOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(browsers);
        options ??= new PlaywrightAutomationStudioOptions();
        var session = browsers.CreateSession(browser => {
            browser.Headless = options.Headless;
            options.ConfigureBrowser?.Invoke(browser);
        });

        var recording = new Recording(session, options.PlanName, options.OutputDirectory, runner);
        try {
            await recording.StartAsync(options.StartUrl, options.PlanPath, ct).ConfigureAwait(false);
            return recording;
        }
        catch {
            await recording.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private sealed class Recording : IPlaywrightAutomationStudio
    {
        private readonly IPlaywrightBrowserSession _session;
        private readonly RecordedEventMapper _mapper;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly TaskCompletionSource<bool> _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly string _outputDir;
        private readonly IAutomationPlanRunner? _runner;
        private readonly List<NetworkHit> _hits = [];
        private readonly List<IPage> _hookedPages = [];
        private AutomationPlan? _plan;
        private string? _loadedPlanJson;
        private bool _paused;
        private bool _capturing;
        private int _playing;
        private readonly List<IPage> _replayPages = [];
        private int _armReplay;
        private int _nextHitId;
        private int _sessionDisposed;
        private int _playStep;
        private bool _grabber;
        private bool _recording;
        private int _recordBinding;
        private AutomationPlanRunGate? _playGate;
        private CancellationTokenSource? _playCts;

        internal Recording(IPlaywrightBrowserSession session, string? planName, string outputDirectory, IAutomationPlanRunner? runner)
        {
            _session = session;
            _mapper = new(planName);
            Browser = session.Browser;
            _runner = runner;
            _outputDir = string.IsNullOrWhiteSpace(outputDirectory) ? Path.Combine(Environment.CurrentDirectory, "plans") : outputDirectory;
        }

        public PlaywrightBrowser Browser { get; }

        public Task Completion => _stopped.Task;

        public AutomationPlan Plan
        {
            get {
                _gate.Wait();
                try {
                    return _mapper.Preview();
                }
                finally {
                    _gate.Release();
                }
            }
        }

        public bool RecordingPaused => _paused;

        public bool IsRecording => _recording;

        public event EventHandler? PlanChanged;

        public async Task StartRecordingAsync(CancellationToken ct = default)
        {
            _recording = true;
            await EnsureRecordBindingAsync().ConfigureAwait(false);
            var page = Browser.Page;
            if (page != null)
                await InstallRecorderAsync(page).ConfigureAwait(false);
        }

        public void StopRecording() => _recording = false;

        public AutomationPlan? LoadedPlan
        {
            get {
                var json = _loadedPlanJson;
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                try {
                    return AutomationPlanJson.Deserialize(json);
                }
                catch (JsonException) {
                    return null;
                }
            }
        }

        public event EventHandler<AutomationStudioStepNotice>? StepNotice;

        /// <summary>Runner log. When set, play writes here in addition to <see cref="StepNotice" />.</summary>
        public ILogger? PlayLogger { get; set; }

        public void PauseRecording() => _paused = true;

        public void ResumeRecording() => _paused = false;

        public void InsertStep(RecordedInsert insert)
        {
            ArgumentHelpers.ThrowIfNull(insert);
            _gate.Wait();
            try {
                _mapper.Apply(new(RecordedDomEventKind.Insert, Insert: insert));
            }
            finally {
                _gate.Release();
            }
        }

        public void MoveStep(int from, int to)
        {
            _gate.Wait();
            try {
                _mapper.Move(from, to);
            }
            finally {
                _gate.Release();
            }
        }

        public void RemoveStep(int index)
        {
            _gate.Wait();
            try {
                _mapper.RemoveAt(index);
            }
            finally {
                _gate.Release();
            }
        }

        public string SavePlan(string? directory = null)
        {
            _gate.Wait();
            try {
                var folder = string.IsNullOrWhiteSpace(directory) ? _outputDir : directory!;
                return WritePlan(folder, _mapper.Build());
            }
            finally {
                _gate.Release();
            }
        }

        public async Task<AutomationPlanRunResult> PlayAsync(AutomationPlan plan, int startStepIndex, string runDirectory, CancellationToken ct = default, int? endStepIndex = null)
        {
            ArgumentHelpers.ThrowIfNull(plan);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(runDirectory);
            if (_runner == null)
                throw new InvalidOperationException("Play is not available.");

            if (Interlocked.Exchange(ref _playing, 1) == 1)
                throw new InvalidOperationException("A plan is already running.");

            _playGate = new();
            _playCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var logger = new StudioStepLogger(PlayLogger, line => Raise(new() { Phase = "log", LogLine = line, StepIndex = _playStep }));
            var instrumentation = new StudioPlayInstrumentation(runDirectory, notice => {
                if (notice.Phase == "start")
                    _playStep = notice.StepIndex;
                Raise(notice);
            });
            var options = new AutomationPlanRuntimeOptions {
                StartStepIndex = startStepIndex,
                EndStepIndex = endStepIndex,
                Gate = _playGate,
                Instrumentation = instrumentation,
                PlanRunDirectory = new() { RootDirectory = runDirectory }
            };
            try {
                return await _runner.RunWithResultAsync(_session, plan, options, logger, _playCts.Token).ConfigureAwait(false);
            }
            finally {
                Volatile.Write(ref _playing, 0);
                _playCts.Dispose();
                _playCts = null;
            }
        }

        public void PausePlay() => _playGate?.Pause();

        public void ResumePlay() => _playGate?.Resume();

        public void CancelPlay() => _playCts?.Cancel();

        public void StartNetworkCapture() => _capturing = true;

        public void StopNetworkCapture() => _capturing = false;

        public void ClearNetwork()
        {
            lock (_hits)
                _hits.Clear();
        }

        public IReadOnlyList<AutomationStudioNetworkRow> NetworkRows(string? urlPattern)
        {
            lock (_hits)
                return _hits.Where(hit => NetworkUrlMatcher.Matches(urlPattern, hit.Url))
                    .Select(hit => new AutomationStudioNetworkRow { Id = hit.Id, Method = hit.Method, Url = hit.Url, Status = hit.Status, ResourceType = hit.ResourceType })
                    .ToList();
        }

        public AutomationStudioNetworkDetail? ReadNetwork(int id)
        {
            var detail = DetailFor(id);
            if (detail == null)
                return null;

            return new() {
                Id = detail.Id,
                Method = detail.Method,
                Url = detail.Url,
                Status = detail.Status,
                RequestBody = detail.RequestBody,
                ResponseBody = detail.ResponseBody,
                RequestHeaders = detail.RequestHeaders,
                ResponseHeaders = detail.ResponseHeaders ?? new Dictionary<string, string>()
            };
        }

        public Task ReplayNetworkAsync(int id, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var replay = CopyReplay(id);
            return replay == null ? Task.CompletedTask : OpenReplayAsync(replay);
        }

        public string? StoreCapturedUrls(string variableName, string? urlPattern)
        {
            if (string.IsNullOrWhiteSpace(variableName))
                return "Type a variable name.";

            try {
                NetworkUrlMatcher.ThrowIfInvalid(urlPattern);
            }
            catch (InvalidOperationException ex) {
                return ex.Message;
            }

            List<string> matched;
            lock (_hits)
                matched = _hits.Select(hit => hit.Url).Where(url => NetworkUrlMatcher.Matches(urlPattern, url)).Distinct(StringComparer.Ordinal).ToList();

            if (matched.Count == 0)
                return "No captured URLs match that pattern.";

            _gate.Wait();
            try {
                _mapper.Apply(new(RecordedDomEventKind.Insert, Insert: new("storeStringList", variableName, Values: matched)));
            }
            finally {
                _gate.Release();
            }

            return null;
        }

        public async Task SetGrabberAsync(bool enabled, CancellationToken ct = default)
        {
            _grabber = enabled;
            var page = Browser.Page;
            if (page == null)
                return;

            if (!enabled) {
                try {
                    await page.EvaluateAsync("() => { if (window.__lyoTool) window.__lyoTool.set(false); }").ConfigureAwait(false);
                }
                catch (PlaywrightException) {
                    // The page navigated away from the tool listener.
                }

                return;
            }

            await InstallToolAsync(page).ConfigureAwait(false);
        }

        public async Task<AutomationStudioElementInfo?> DescribeElementAsync(CancellationToken ct = default)
        {
            var page = Browser.Page;
            if (page == null)
                return null;

            string? json;
            try {
                json = await page.EvaluateAsync<string>("() => { const row = window.__lyoTool && window.__lyoTool.describe(); return row ? JSON.stringify(row) : null; }")
                    .ConfigureAwait(false);
            }
            catch (PlaywrightException) {
                return null;
            }

            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<AutomationStudioElementInfo>(json, MessageOptions);
        }

        public async Task HighlightAsync(ElementLocator locator, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            ArgumentHelpers.ThrowIfNull(locator);
            var page = Browser.Page;
            if (page == null)
                return;

            try {
                await PlaywrightLocatorFactory.Locate(page, locator).EvaluateAsync(HighlightSource).ConfigureAwait(false);
            }
            catch (PlaywrightException) {
                // The element was gone before the outline was applied.
            }
        }

        public async Task<string?> InsertGrabbedAsync(string tool, string variableName, CancellationToken ct = default)
        {
            var page = Browser.Page;
            if (page == null)
                return "The browser is not running.";

            string? json;
            try {
                json = await page.EvaluateAsync<string>(
                    "({ tool, variable }) => { const row = window.__lyoTool && window.__lyoTool.grab(tool, variable); return row ? JSON.stringify(row) : null; }",
                    new Dictionary<string, object?> { ["tool"] = tool, ["variable"] = variableName }).ConfigureAwait(false);
            }
            catch (PlaywrightException) {
                return "The info grabber is not on this page.";
            }

            if (string.IsNullOrWhiteSpace(json))
                return "No element is under the cursor.";

            var pick = JsonSerializer.Deserialize<GrabPick>(json, MessageOptions);
            if (pick == null || !Enum.TryParse<RecordedPickTool>(pick.Tool, true, out var parsed))
                return "That grab did not name a tool.";

            var locator = ToLocator(pick.Locator);
            var parent = ToLocator(pick.ParentLocator);
            _gate.Wait(ct);
            try {
                _mapper.Apply(new(
                    RecordedDomEventKind.Pick,
                    Locator: locator,
                    Pick: new(parsed, pick.Repeating, parent, pick.DescendantTag, locator, pick.VariableName ?? variableName, pick.AttributeName)));
            }
            finally {
                _gate.Release();
            }

            return null;
        }

        private async Task EnsureRecordBindingAsync()
        {
            if (Interlocked.Exchange(ref _recordBinding, 1) == 1)
                return;

            var context = Browser.Context ?? throw new InvalidOperationException("Playwright context was not created.");
            await context.ExposeBindingAsync("__lyoRecord", (BindingSource _, string payload) => OnRecorded(payload)).ConfigureAwait(false);
        }

        private async Task InstallRecorderAsync(IPage page)
        {
            try {
                await page.EvaluateAsync(StudioRecorderPage.Source).ConfigureAwait(false);
            }
            catch (PlaywrightException) {
                // The document went away before the recorder was installed.
            }
        }

        private void OnRecorded(string payload)
        {
            if (!_recording || Volatile.Read(ref _playing) == 1 || string.IsNullOrWhiteSpace(payload))
                return;

            RecordedPayload? message;
            try {
                message = JsonSerializer.Deserialize<RecordedPayload>(payload, MessageOptions);
            }
            catch (JsonException) {
                return;
            }

            if (message == null || !Enum.TryParse<RecordedDomEventKind>(message.Kind, out var kind))
                return;

            ElementLocator? locator = null;
            if (message.Locator is { Kind: { } kindName, Value: { } value } && Enum.TryParse<ElementLocatorKind>(kindName, out var locatorKind) && !string.IsNullOrWhiteSpace(value))
                locator = new(locatorKind, value);

            ApplyRecorded(new(kind, message.Key, locator, Text: message.Text, Value: message.Value, Url: message.Url));
        }

        private void ApplyRecorded(RecordedDomEvent evt)
        {
            _gate.Wait();
            try {
                _mapper.Apply(evt);
            }
            finally {
                _gate.Release();
            }

            PlanChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task InstallToolAsync(IPage page)
        {
            try {
                await page.EvaluateAsync(StudioToolPage.Source).ConfigureAwait(false);
            }
            catch (PlaywrightException) {
                // The document went away before the tool listener was installed.
            }
        }

        private void Raise(AutomationStudioStepNotice notice) => StepNotice?.Invoke(this, notice);

        internal async Task StartAsync(string? startUrl, string? planPath, CancellationToken ct)
        {
            string? planJson = null;
            if (planPath is string planFile && !string.IsNullOrWhiteSpace(planFile))
                planJson = File.ReadAllText(planFile);

            await _session.StartBrowserAsync(ct).ConfigureAwait(false);
            var context = Browser.Context ?? throw new InvalidOperationException("Playwright context was not created.");
            var page = Browser.Page ?? throw new InvalidOperationException("Playwright page was not created.");
            if (startUrl is string start && !string.IsNullOrWhiteSpace(start))
                await Browser.NavigateToAsync(start, ct).ConfigureAwait(false);

            if (planJson is not null) {
                _loadedPlanJson = planJson;
                _mapper.ApplyJson(planJson);
            }

            context.Request += OnContextRequest;
            context.Response += OnContextResponse;
            context.Page += OnContextPage;
            HookPage(page);
        }

        public async Task<AutomationPlan> StopAsync(CancellationToken ct = default)
        {
            CancelPlay();
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try {
                _plan ??= _mapper.Build();
            }
            finally {
                _gate.Release();
            }

            SignalStopped();
            await DisposeSessionAsync().ConfigureAwait(false);
            return _plan;
        }

        public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

        private static string WritePlan(string directory, AutomationPlan plan)
        {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(directory);
            Directory.CreateDirectory(directory);
            var stem = SanitizeFileName(plan.Name);
            var path = Path.Combine(directory, $"{stem}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.json");
            File.WriteAllText(path, AutomationPlanJson.Serialize(plan));
            return path;
        }

        private static string SanitizeFileName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "plan";

            var invalid = Path.GetInvalidFileNameChars();
            var chars = name.Select(ch => Array.IndexOf(invalid, ch) >= 0 ? '-' : ch).ToArray();
            var text = new string(chars).Trim();
            return string.IsNullOrWhiteSpace(text) ? "plan" : text;
        }

        private void OnFrameNavigated(object? sender, IFrame frame)
        {
            if (frame.ParentFrame != null)
                return;

            var page = frame.Page;
            if (page == null || IsReplay(page))
                return;

            if (_recording) {
                _ = InstallRecorderAsync(page);
                if (!string.IsNullOrWhiteSpace(frame.Url))
                    ApplyRecorded(new(RecordedDomEventKind.Navigate, Url: frame.Url));
            }

            if (_grabber)
                _ = InstallToolAsync(page);
        }

        private void OnPageClosed(object? sender, IPage page)
        {
            try {
                var context = Browser.Context;
                if (context != null && context.Pages.Any(candidate => !candidate.IsClosed && !ReferenceEquals(candidate, page)))
                    return;
            }
            catch (PlaywrightException) {
                return;
            }

            SignalStopped();
        }

        private void SignalStopped() => _stopped.TrySetResult(true);

        private async Task DisposeSessionAsync()
        {
            if (Interlocked.Exchange(ref _sessionDisposed, 1) != 0)
                return;

            try {
                await _session.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is PlaywrightException or ObjectDisposedException) {
                // The window was already closed.
            }
        }

        private void OnContextPage(object? sender, IPage page)
        {
            if (Interlocked.Exchange(ref _armReplay, 0) == 1) {
                lock (_replayPages)
                    _replayPages.Add(page);
            }

            HookPage(page);
        }

        private void HookPage(IPage? page)
        {
            if (page == null)
                return;

            lock (_hookedPages) {
                foreach (var existing in _hookedPages) {
                    if (!ReferenceEquals(existing, page))
                        continue;

                    return;
                }

                _hookedPages.Add(page);
            }

            page.FrameNavigated += OnFrameNavigated;
            page.Close += OnPageClosed;
        }

        private void OnContextRequest(object? sender, IRequest request)
        {
            if (!_capturing || request.Url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return;

            string? body = null;
            try {
                body = request.PostData;
            }
            catch (PlaywrightException) {
                body = null;
            }

            if (body is { Length: > NetworkBodyFormatter.TextLimit })
                body = body.Substring(0, NetworkBodyFormatter.TextLimit);

            var hit = new NetworkHit {
                Id = Interlocked.Increment(ref _nextHitId),
                Request = request,
                Method = request.Method,
                Url = request.Url,
                ResourceType = request.ResourceType ?? "",
                PostData = body,
                RequestHeaders = CopyHeaders(request.Headers)
            };

            lock (_hits) {
                _hits.Add(hit);
                if (_hits.Count > 300)
                    _hits.RemoveAt(0);
            }
        }

        private void OnContextResponse(object? sender, IResponse response)
        {
            if (!_capturing)
                return;

            NetworkHit? hit = null;
            lock (_hits) {
                for (var i = _hits.Count - 1; i >= 0; i--) {
                    if (!ReferenceEquals(_hits[i].Request, response.Request))
                        continue;

                    hit = _hits[i];
                    break;
                }
            }

            if (hit == null)
                return;

            try {
                hit.Status = response.Status;
                hit.ResponseHeaders = CopyHeaders(response.Headers);
                hit.ContentType = HeaderValue(hit.ResponseHeaders, "content-type");
            }
            catch (PlaywrightException) {
                hit.BodyReady = true;
                return;
            }

            if (SkipBody(hit)) {
                hit.BodyBytes = 0;
                hit.BodyReady = true;
                hit.ResponseKind = "empty";
                return;
            }

            _ = Task.Run(() => ReadBodyAsync(hit, response));
        }

        private static bool SkipBody(NetworkHit hit)
        {
            if (hit.Status is 204 or 304)
                return true;

            if (string.Equals(hit.Method, "HEAD", StringComparison.OrdinalIgnoreCase))
                return true;

            return HeaderValue(hit.ResponseHeaders, "content-length") == "0";
        }

        private async Task ReadBodyAsync(NetworkHit hit, IResponse response)
        {
            try {
                var bytes = await response.BodyAsync().ConfigureAwait(false);
                var formatted = NetworkBodyFormatter.Format(hit.ContentType, bytes);
                lock (_hits) {
                    hit.BodyBytes = bytes.Length;
                    hit.BodyReady = true;
                    hit.BodyTruncated = formatted.Truncated;
                    hit.ResponseKind = formatted.Kind;
                    hit.ResponseText = formatted.Text;
                    hit.ImageDataUrl = formatted.ImageDataUrl;
                }
            }
            catch (Exception ex) when (ex is PlaywrightException or ObjectDisposedException) {
                lock (_hits)
                    hit.BodyReady = true;
            }
        }

        private static string? HeaderValue(IReadOnlyDictionary<string, string>? headers, string name)
        {
            if (headers == null)
                return null;

            foreach (var pair in headers) {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }

            return null;
        }

        private static Dictionary<string, string> CopyHeaders(IDictionary<string, string>? headers)
        {
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headers == null)
                return copy;

            var count = 0;
            foreach (var pair in headers) {
                if (count >= 40)
                    break;

                count++;
                var value = pair.Value ?? "";
                copy[pair.Key] = value.Length > 500 ? value.Substring(0, 500) : value;
            }

            return copy;
        }

        private bool IsReplay(IPage? page)
        {
            if (page == null)
                return false;

            lock (_replayPages)
                return _replayPages.Exists(candidate => ReferenceEquals(candidate, page));
        }

        private ReplayRequest? CopyReplay(int id)
        {
            lock (_hits) {
                foreach (var hit in _hits) {
                    if (hit.Id != id)
                        continue;

                    return new(hit.Method, hit.Url, HeaderValue(hit.RequestHeaders, "content-type"), hit.PostData);
                }
            }

            return null;
        }

        private async Task OpenReplayAsync(ReplayRequest replay)
        {
            try {
                var context = Browser.Context;
                if (context == null)
                    return;

                Interlocked.Exchange(ref _armReplay, 1);
                var page = await context.NewPageAsync().ConfigureAwait(false);
                lock (_replayPages) {
                    if (!_replayPages.Exists(candidate => ReferenceEquals(candidate, page)))
                        _replayPages.Add(page);
                }

                page.Close += (_, closed) => {
                    lock (_replayPages)
                        _replayPages.RemoveAll(candidate => ReferenceEquals(candidate, closed));
                };

                if (replay.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) || replay.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase)) {
                    await page.GotoAsync(replay.Url, new() { Timeout = 30_000 }).ConfigureAwait(false);
                    return;
                }

                await page.SetContentAsync("<pre id=\"lyo-replay\"></pre>").ConfigureAwait(false);
                await page.EvaluateAsync(
                    """
                    async ({url, method, contentType, body}) => {
                      const headers = {};
                      if (contentType) headers["content-type"] = contentType;
                      const response = await fetch(url, { method, headers, body: body || undefined, credentials: "include" });
                      const text = await response.text();
                      const node = document.getElementById("lyo-replay");
                      if (node) node.textContent = response.status + " " + response.statusText + "\n\n" + text;
                    }
                    """,
                    new Dictionary<string, object?> {
                        ["url"] = replay.Url,
                        ["method"] = replay.Method,
                        ["contentType"] = replay.ContentType,
                        ["body"] = replay.Body
                    }).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is PlaywrightException or ObjectDisposedException) {
                // The new tab can be closed while the resent request is still running.
            }
        }

        private NetworkDetail? DetailFor(int id)
        {
            lock (_hits) {
                NetworkHit? hit = null;
                foreach (var candidate in _hits) {
                    if (candidate.Id != id)
                        continue;

                    hit = candidate;
                    break;
                }

                if (hit == null)
                    return null;

                var requestType = HeaderValue(hit.RequestHeaders, "content-type");
                var requestBody = NetworkBodyFormatter.FormatText(requestType, hit.PostData);
                return new(
                    hit.Id,
                    hit.Method,
                    hit.Url,
                    hit.ResourceType,
                    hit.Status,
                    hit.ContentType,
                    hit.BodyBytes,
                    hit.BodyReady,
                    hit.BodyTruncated,
                    requestBody.Kind,
                    requestBody.Text,
                    hit.ResponseKind,
                    hit.ResponseText,
                    hit.ImageDataUrl,
                    hit.RequestHeaders,
                    hit.ResponseHeaders);
            }
        }

        private static ElementLocator? ToLocator(LocatorMessage? message)
        {
            if (message?.Value is not string value || string.IsNullOrWhiteSpace(value) || !Enum.TryParse<ElementLocatorKind>(message.Kind, true, out var kind))
                return null;

            return new(kind, value);
        }

        private sealed class GrabPick
        {
            public string? Tool { get; set; }

            public string? VariableName { get; set; }

            public bool Repeating { get; set; }

            public LocatorMessage? ParentLocator { get; set; }

            public string? DescendantTag { get; set; }

            public LocatorMessage? Locator { get; set; }

            public string? AttributeName { get; set; }
        }

        private sealed class StudioStepLogger(ILogger? inner, Action<string> write) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state)
                where TState : notnull
                => inner?.BeginScope(state) ?? Empty.Instance;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                    return;

                var line = formatter(state, exception);
                write(line);
                inner?.Log(logLevel, eventId, state, exception, formatter);
            }

            private sealed class Empty : IDisposable
            {
                public static readonly Empty Instance = new();

                public void Dispose() { }
            }
        }

        private sealed class StudioPlayInstrumentation(string runDirectory, Action<AutomationStudioStepNotice> raise) : AutomationPlanInstrumentationBase
        {
            private Guid _runId;

            public override void OnRunStarted(in AutomationPlanRunStartedEvent e) => _runId = e.RunId;

            public override void OnStepStarting(in AutomationPlanStepStartingEvent e)
                => raise(new() { Phase = "start", StepIndex = e.StepIndex, StepExecutionId = e.StepExecutionId, Label = e.Step.ToString() });

            public override void OnStepOutcome(in AutomationPlanStepOutcomeRecord e)
            {
                string? screenshot = null;
                var shot = e.Outcome switch {
                    AutomationPlanStepOutcome.Success => "after",
                    AutomationPlanStepOutcome.Failed or AutomationPlanStepOutcome.TimedOut => "failed",
                    _ => null
                };
                if (shot != null) {
                    var path = Path.Combine(runDirectory, _runId.ToString("N"), "snapshots", $"{e.StepIndex:000}_{e.StepExecutionId:N}_{shot}.png");
                    if (File.Exists(path))
                        screenshot = path;
                }

                raise(new() {
                    Phase = "outcome",
                    StepIndex = e.StepIndex,
                    StepExecutionId = e.StepExecutionId,
                    Outcome = e.Outcome,
                    Error = e.Error?.Message,
                    Duration = e.Duration,
                    ScreenshotPath = screenshot,
                    Context = e.Context
                });
            }
        }

        private sealed class RecordedPayload
        {
            public string? Kind { get; set; }

            public string? Key { get; set; }

            public string? Text { get; set; }

            public string? Value { get; set; }

            public string? Url { get; set; }

            public LocatorMessage? Locator { get; set; }
        }

        private sealed class LocatorMessage
        {
            public string? Kind { get; set; }

            public string? Value { get; set; }
        }

        private sealed class NetworkHit
        {
            public int Id { get; init; }

            public IRequest Request { get; init; } = null!;

            public string Method { get; init; } = "";

            public string Url { get; init; } = "";

            public string ResourceType { get; init; } = "";

            public string? PostData { get; set; }

            public Dictionary<string, string> RequestHeaders { get; init; } = [];

            public int? Status { get; set; }

            public string? ContentType { get; set; }

            public int? BodyBytes { get; set; }

            public bool BodyReady { get; set; }

            public bool BodyTruncated { get; set; }

            public string? ResponseKind { get; set; }

            public string? ResponseText { get; set; }

            public string? ImageDataUrl { get; set; }

            public Dictionary<string, string>? ResponseHeaders { get; set; }
        }

        private sealed record ReplayRequest(string Method, string Url, string? ContentType, string? Body);

        private sealed record NetworkRow(int Id, string Method, int? Status, string Url);

        private sealed record NetworkDetail(
            int Id,
            string Method,
            string Url,
            string ResourceType,
            int? Status,
            string? ContentType,
            int? BodyBytes,
            bool BodyReady,
            bool BodyTruncated,
            string RequestKind,
            string? RequestBody,
            string? ResponseKind,
            string? ResponseBody,
            string? ImageDataUrl,
            IReadOnlyDictionary<string, string> RequestHeaders,
            IReadOnlyDictionary<string, string>? ResponseHeaders);
    }
}
