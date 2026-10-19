using System.Security.Claims;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Web.Components.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Lyo.TestGateway.Components.Layout;

public partial class MainLayout
{
    private ClaimsPrincipal? user;
    private string? _displayName;
    private string? _email;
    private string _initials = string.Empty;

    /// <summary>App-bar title. Pages assign it via the cascading layout instance.</summary>
    public string PageName { get; set; } = "";

    /// <summary>Omits registry groups that the drawer already lists manually.</summary>
    private static readonly string[] RegistrySkip = ["Infrastructure"];

    private string _navQuery = "";

    private readonly HashSet<string> _openGroups = new(StringComparer.OrdinalIgnoreCase);

    private static readonly NavLink[] Pinned = [
        new("Home", "/", Icons.Material.Filled.Home, NavLinkMatch.All),
        new("Admin", "AdminDashboard", Icons.Material.Filled.AdminPanelSettings)
    ];

    private static readonly NavGroup AccountGroup = new("Account", Icons.Material.Filled.AccountCircle, [
        new("API tokens", "auth/tokens", Icons.Material.Filled.VpnKey),
        new("Auth admin", "auth/admin", Icons.Material.Filled.Security),
        new("Auth debug", "auth/debug", Icons.Material.Filled.BugReport),
        new("Profile", "auth/profile", Icons.Material.Filled.Person)
    ]);

    private static readonly NavGroup[] NavGroups = [
        new("People", Icons.Material.Filled.Contacts, [
            new("Endato", Constants.Page.Endato, Icons.Material.Filled.PersonSearch),
            new("People", "PeopleManagement", Icons.Material.Filled.Contacts)
        ]),
        new("Communication", Icons.Material.Filled.Forum, [
            new("Profanity", Constants.Page.Profanity, Icons.Material.Filled.Warning),
            new("Fantasy football", Constants.Page.FantasyFootball, Icons.Material.Filled.SportsFootball),
            new("SMS / email", Constants.Page.Messaging, Icons.Material.Filled.Textsms),
            new("Translate", Constants.Page.Translation, Icons.Material.Filled.Translate),
            new("TTS", Constants.Page.Tts, Icons.Material.Filled.AudioFile)
        ]),
        new("Data", Icons.Material.Filled.TableChart, [
            new("CSV / XLSX", Constants.Page.CsvXlsx, Icons.Material.Filled.TableChart),
            new("Formatter", Constants.Page.Formatter, Icons.Material.Filled.TextFields),
            new("ID generator", Constants.Page.IdGenerator, Icons.Material.Filled.Tag),
            new("Privacy redaction", Constants.Page.PrivacyRedaction, Icons.Material.Filled.PrivacyTip),
            new("Query builder", Constants.Page.QueryBuilder, Icons.Material.Filled.QueryBuilder),
            new("Text diff", Constants.Page.TextDiff, Icons.Material.Filled.CompareArrows)
        ]),
        new("Files", Icons.Material.Filled.Folder, [
            new("Encrypt / compress", Constants.Page.FileService, Icons.Material.Filled.EnhancedEncryption),
            new("File storage", Constants.Page.FileStorageWorkbench, Icons.Material.Filled.Storage),
            new("Key store", Constants.Page.KeyStoreWorkbench, Icons.Material.Filled.VpnKey)
        ]),
        new("Documents", Icons.Material.Filled.Description, [
            new("HTML to PDF", Constants.Page.HtmlToPdf, Icons.Material.Filled.PictureAsPdf),
            new("PDF annotator", Constants.Page.PdfAnnotator, Icons.Material.Filled.PictureAsPdf),
            new("Rich text", Constants.Page.RichTextEditor, Icons.Material.Filled.Article)
        ]),
        new("Media", Icons.Material.Filled.PermMedia, [
            new("Barcode", Constants.Page.BarcodeGenerator, Icons.Material.Filled.ViewWeek),
            new("Image", Constants.Page.ImageWorkbench, Icons.Material.Filled.Palette),
            new("QR code", Constants.Page.QrCodeGenerator, Icons.Material.Filled.QrCode),
            new("Spritesheet", Constants.Page.SpriteSheetAnimator, Icons.Material.Filled.Movie)
        ]),
        new("Apps", Icons.Material.Filled.Apps, [
            new("Billing", Constants.Page.Billing, Icons.Material.Filled.Receipt),
            new("Home inventory", Constants.Page.HomeInventory, Icons.Material.Filled.Home),
            new("Recipe", Constants.Page.Recipe, Icons.Material.Filled.RestaurantMenu),
            new("Story", Constants.Page.Story, Icons.Material.Filled.AutoStories),
            new("Leaderboard", Constants.Page.Leaderboard, Icons.Material.Filled.EmojiEvents),
            new("Report design", Constants.Page.ReportDesign, Icons.Material.Filled.DashboardCustomize),
            new("Reports", Constants.Page.Reports, Icons.Material.Filled.Assessment)
        ]),
        new("Automation", Icons.Material.Filled.AccountTree, [
            new("Jobs", Constants.Page.Jobs, Icons.Material.Filled.Work),
            new("Schedule", Constants.Page.Schedule, Icons.Material.Filled.Schedule),
            new("Web automation", Constants.Page.Automation, Icons.Material.Filled.Language),
            new("Workflow", Constants.Page.WorkflowBuilder, Icons.Material.Filled.AccountTree)
        ]),
        new("Infrastructure", Icons.Material.Filled.SettingsEthernet, [
            new("Cache", Constants.Page.Cache, Icons.Material.Filled.Storage),
            new("Config", Constants.Page.Config, Icons.Material.Filled.Tune),
            new("Diagnostics", Constants.Page.Diagnostics, Icons.Material.Filled.BugReport),
            new("Drift", Constants.Page.Drift, Icons.Material.Filled.CompareArrows),
            new("Locks", Constants.Page.Locks, Icons.Material.Filled.Lock),
            new("Metrics", Constants.Page.Metrics, Icons.Material.Filled.HourglassTop),
            new("Package catalog", Constants.Page.Catalog, Icons.Material.Filled.Inventory2),
            new("RabbitMQ", Constants.Page.RabbitMq, Icons.Material.Filled.Message),
            new("Seed", Constants.Page.Seed, Icons.Material.Filled.Science)
        ])
    ];

    protected override async Task OnInitializedAsync()
    {
        AuthenticationStateProvider.AuthenticationStateChanged += OnAuthStateChanged;
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        ApplyUser(authState.User);
    }

    private void OnAuthStateChanged(Task<AuthenticationState> task)
        => _ = task.ContinueWith(
            t => InvokeAsync(() => {
                ApplyUser(t.Result.User);
                StateHasChanged();
            }), TaskScheduler.Default);

    private void ApplyUser(ClaimsPrincipal principal)
    {
        user = principal;
        if (principal.Identity?.IsAuthenticated != true) {
            _displayName = null;
            _email = null;
            _initials = string.Empty;
            return;
        }

        _displayName = principal.Identity!.Name ?? principal.FindFirst(LyoJwtClaims.Subject)?.Value ?? principal.FindFirst(LyoJwtClaims.LyoUser)?.Value;
        _email = principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("email")?.Value;
        _initials = BuildInitials(_displayName ?? _email);
    }

    private static string BuildInitials(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;

        var trimmed = source.Trim();
        var atIdx = trimmed.IndexOf('@');
        var basis = atIdx > 0 ? trimmed[..atIdx] : trimmed;
        var parts = basis.Split([' ', '.', '-', '_'], StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch {
            0 => trimmed[..1].ToUpperInvariant(),
            1 => parts[0][..1].ToUpperInvariant(),
            var _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
        };
    }

    private async Task SignOutAsync()
    {
        try {
            await SignInLauncher.SignOutAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Sign-out failed: {ex.Message}", Severity.Error);
        }
    }

    public void Dispose() => AuthenticationStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;

    private bool IsAuthenticated => user?.Identity?.IsAuthenticated == true;

    private bool Filtering => !string.IsNullOrWhiteSpace(_navQuery);

    private bool ShowSignIn => !IsAuthenticated && (!Filtering || Matches("Sign in"));

    private bool HasVisibleNav
        => Visible(Pinned, group: null).Any()
            || ShowSignIn
            || (IsAuthenticated && Visible(AccountGroup.Items, AccountGroup.Title).Any())
            || NavGroups.Any(group => Visible(group.Items, group.Title).Any());

    private bool Matches(string text) => text.Contains(_navQuery.Trim(), StringComparison.OrdinalIgnoreCase);

    private IEnumerable<NavLink> Visible(IReadOnlyList<NavLink> items, string? group)
    {
        foreach (var item in items) {
            if (Filtering && !Matches(item.Title) && (group is null || !Matches(group)))
                continue;
            yield return item;
        }
    }

    private bool IsGroupOpen(NavGroup group)
        => (Filtering && Visible(group.Items, group.Title).Any())
            || group.Items.Any(item => RouteIs(item.Href))
            || _openGroups.Contains(group.Title);

    private void SetGroupOpen(NavGroup group, bool open)
    {
        if (open)
            _openGroups.Add(group.Title);
        else
            _openGroups.Remove(group.Title);
    }

    private bool RouteIs(string href)
    {
        var current = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).Trim('/');
        var target = href.Trim('/');
        if (target.Length == 0)
            return current.Length == 0;

        return current.Equals(target, StringComparison.OrdinalIgnoreCase)
            || current.StartsWith(target + "/", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record NavLink(string Title, string Href, string Icon, NavLinkMatch Match = NavLinkMatch.Prefix);

    private sealed record NavGroup(string Title, string Icon, NavLink[] Items);
}
