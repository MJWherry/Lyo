using System.Text;
using System.Text.Json;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Plan;

/// <summary>Per-run files under <see cref="AutomationPlanRunDirectoryOptions.RootDirectory" /> (log transcript, PNGs, variable JSON).</summary>
internal sealed class AutomationPlanRunArtifacts : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly object _logGate = new();
    private readonly StreamWriter? _logWriter;

    private readonly AutomationPlanRunDirectoryOptions _options;

    public string RunRoot { get; }

    public string LogsDirectory { get; }

    public string SnapshotsDirectory { get; }

    public string VariablesDirectory { get; }

    private AutomationPlanRunArtifacts(AutomationPlanRunDirectoryOptions options, string runRoot, string logsDir, string snapshotsDir, string variablesDir, StreamWriter? logWriter)
    {
        _options = options;
        RunRoot = runRoot;
        LogsDirectory = logsDir;
        SnapshotsDirectory = snapshotsDir;
        VariablesDirectory = variablesDir;
        _logWriter = logWriter;
    }

    public void Dispose() => _logWriter?.Dispose();

    public static AutomationPlanRunArtifacts? TryCreate(AutomationPlanRunDirectoryOptions directoryOptions, Guid runId)
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(directoryOptions.RootDirectory), "PlanRunDirectory.RootDirectory must be non-empty.", nameof(directoryOptions));
        var root = Path.GetFullPath(directoryOptions.RootDirectory);
        var runRoot = directoryOptions.NestRunUnderRoot ? Path.Combine(root, directoryOptions.RunFolderName ?? runId.ToString("N")) : root;
        var logsDir = Path.Combine(runRoot, directoryOptions.LogsSubdirectory);
        var snapshotsDir = Path.Combine(runRoot, directoryOptions.SnapshotsSubdirectory);
        var variablesDir = Path.Combine(runRoot, directoryOptions.VariablesSubdirectory);
        Directory.CreateDirectory(runRoot);
        if (directoryOptions.WriteRunLogFile)
            Directory.CreateDirectory(logsDir);

        if (directoryOptions.WriteSnapshots)
            Directory.CreateDirectory(snapshotsDir);

        if (directoryOptions.WriteVariables)
            Directory.CreateDirectory(variablesDir);

        StreamWriter? logWriter = null;
        if (directoryOptions.WriteRunLogFile) {
            var logPath = Path.Combine(logsDir, SanitizeFileName(directoryOptions.RunLogFileName));
            logWriter = new(
                new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous), new UTF8Encoding(false)) { AutoFlush = true };
        }

        return new(directoryOptions, runRoot, logsDir, snapshotsDir, variablesDir, logWriter);
    }

    public void LogLine(string line)
    {
        if (_logWriter == null)
            return;

        lock (_logGate)
            _logWriter.WriteLine($"{DateTime.UtcNow:O}\t{line}");
    }

    public async Task TryWriteVariablesAsync(
        string relativeFileName,
        Guid runId,
        int? stepIndex,
        string phase,
        IReadOnlyDictionary<string, string> strings,
        IReadOnlyDictionary<string, IReadOnlyList<string>> stringLists,
        ILogger? logger,
        CancellationToken ct)
    {
        if (!_options.WriteVariables)
            return;

        try {
            var safeName = SanitizeFileName(relativeFileName);
            var path = Path.Combine(VariablesDirectory, safeName);
            var json = JsonSerializer.Serialize(
                new VariableDumpDto(
                    runId, stepIndex, phase, strings.ToDictionary(static x => x.Key, static x => x.Value, StringComparer.Ordinal),
                    stringLists.ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value is List<string> l ? l : kvp.Value.ToList(), StringComparer.Ordinal)),
                SerializerOptions);

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous)) {
                using (var w = new StreamWriter(fs, new UTF8Encoding(false)))
                    await w.WriteAsync(json).ConfigureAwait(false);
            }

            logger?.LogInformation("Automation plan variables written {AutomationVariablesPath} phase={VariablesPhase} stepIndex={StepIndex}", path, phase, stepIndex);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception ex) {
            logger?.LogWarning(ex, "Automation plan variables write failed phase={VariablesPhase} stepIndex={StepIndex}", phase, stepIndex);
        }
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "unnamed";

        var chars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(value.Length);
        foreach (var c in value) {
            if (Array.IndexOf(chars, c) >= 0)
                sb.Append('_');
            else
                sb.Append(c);
        }

        var s = sb.ToString();
        return s.Length == 0 ? "unnamed" : s;
    }

    private sealed record VariableDumpDto(Guid RunId, int? StepIndex, string Phase, Dictionary<string, string> Strings, Dictionary<string, List<string>> StringLists);
}