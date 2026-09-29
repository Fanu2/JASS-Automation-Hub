using System.Diagnostics;
using System.Net.Http;
using JASS.AutomationHub.Models;

namespace JASS.AutomationHub.Services;

public sealed class WorkflowEngine
{
    private readonly HttpClient _http = new();

    public async Task<RunResult> RunAsync(
        Automation automation,
        bool dryRun,
        IProgress<string>? progress = null,
        CancellationToken token = default)
    {
        var result = new RunResult();
        var variables = automation.Variables.ToDictionary(v => v.Name, v => v.Value, StringComparer.OrdinalIgnoreCase);
        var byId = automation.Nodes.ToDictionary(n => n.Id);
        var outgoing = automation.Connections.GroupBy(c => c.FromNodeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var current = automation.Nodes.FirstOrDefault(n => n.Kind == ActionKind.Start) ?? automation.Nodes.OrderBy(n => n.Y).FirstOrDefault();
        var visited = new Dictionary<Guid, int>();
        var guard = Math.Max(100, automation.Nodes.Count * 20);

        while (current != null && guard-- > 0)
        {
            token.ThrowIfCancellationRequested();
            if (!current.Enabled)
            {
                current = Next(current, outgoing, null, byId);
                continue;
            }

            visited[current.Id] = visited.TryGetValue(current.Id, out var count) ? count + 1 : 1;
            if (visited[current.Id] > 50)
            {
                result.Success = false;
                result.Error = $"Loop safety limit reached at '{current.Title}'.";
                progress?.Report($"✕ Loop safety limit reached: {current.Title}");
                break;
            }

            var parameter = Expand(current.Parameter, variables);
            progress?.Report($"▶ {current.Title}");

            var ok = false;
            Exception? lastError = null;
            var attempts = Math.Max(1, current.RetryCount + 1);

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    if (current.Kind == ActionKind.IfCondition)
                    {
                        ok = Evaluate(current.Condition, variables);
                        progress?.Report(ok ? "  Condition TRUE" : "  Condition FALSE");
                    }
                    else if (current.Kind == ActionKind.LoopEnd)
                    {
                        ok = true;
                    }
                    else
                    {
                        if (!dryRun)
                            await ExecuteNodeAsync(current, parameter, variables, progress, token);
                        else
                            progress?.Report($"  DRY RUN: {Describe(current, parameter)}");
                        ok = true;
                    }
                    break;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    progress?.Report($"  Attempt {attempt}/{attempts} failed: {ex.Message}");
                    if (attempt < attempts) await Task.Delay(400, token);
                }
            }

            if (!ok)
            {
                result.Success = false;
                result.Error = lastError?.Message ?? $"Action failed: {current.Title}";
                break;
            }

            result.CompletedSteps++;

            string? label = current.Kind == ActionKind.IfCondition ? (Evaluate(current.Condition, variables) ? "True" : "False") : null;
            current = Next(current, outgoing, label, byId);
        }

        if (guard <= 0 && result.Success)
        {
            result.Success = false;
            result.Error = "Workflow execution guard reached.";
        }

        return result;
    }

    private static WorkflowNode? Next(
        WorkflowNode node,
        Dictionary<Guid, List<WorkflowConnection>> outgoing,
        string? label,
        Dictionary<Guid, WorkflowNode> byId)
    {
        if (!outgoing.TryGetValue(node.Id, out var links) || links.Count == 0) return null;
        var chosen = label == null
            ? links.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Label)) ?? links.First()
            : links.FirstOrDefault(x => string.Equals(x.Label, label, StringComparison.OrdinalIgnoreCase))
              ?? links.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Label));
        return chosen != null && byId.TryGetValue(chosen.ToNodeId, out var target) ? target : null;
    }

    private async Task ExecuteNodeAsync(
        WorkflowNode node,
        string parameter,
        Dictionary<string, string> variables,
        IProgress<string>? progress,
        CancellationToken token)
    {
        switch (node.Kind)
        {
            case ActionKind.Start:
                await Task.Yield();
                break;
            case ActionKind.CreateFolder:
                Directory.CreateDirectory(parameter);
                break;
            case ActionKind.MoveFile:
            case ActionKind.RenameFile:
            {
                var parts = Split(parameter);
                if (parts.Length != 2) throw new InvalidOperationException("Use Source => Destination.");
                File.Move(parts[0], parts[1], true);
                break;
            }
            case ActionKind.CopyFile:
            {
                var parts = Split(parameter);
                if (parts.Length != 2) throw new InvalidOperationException("Use Source => Destination.");
                File.Copy(parts[0], parts[1], true);
                break;
            }
            case ActionKind.DeleteFile:
                File.Delete(parameter);
                break;
            case ActionKind.WriteLog:
                await File.AppendAllTextAsync(
                    Path.Combine(App.DataFolder, "automation.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {parameter}{Environment.NewLine}", token);
                break;
            case ActionKind.RunProcess:
            {
                using var process = Process.Start(new ProcessStartInfo { FileName = parameter, UseShellExecute = true });
                if (process != null) await process.WaitForExitAsync(token);
                break;
            }
            case ActionKind.HttpGet:
                using (var response = await _http.GetAsync(parameter, token))
                    response.EnsureSuccessStatusCode();
                break;
            case ActionKind.Delay:
                if (!int.TryParse(parameter, out var ms)) throw new InvalidOperationException("Delay must be milliseconds.");
                await Task.Delay(ms, token);
                break;
            case ActionKind.SetVariable:
            {
                var parts = Split(parameter);
                if (parts.Length != 2) throw new InvalidOperationException("Use Variable => Value.");
                variables[parts[0]] = parts[1];
                progress?.Report($"  {parts[0]} = {parts[1]}");
                break;
            }
            case ActionKind.ForEachFile:
            {
                if (!Directory.Exists(parameter)) throw new DirectoryNotFoundException(parameter);
                foreach (var file in Directory.EnumerateFiles(parameter))
                {
                    token.ThrowIfCancellationRequested();
                    variables["CurrentFile"] = file;
                    progress?.Report($"  CurrentFile = {Path.GetFileName(file)}");
                }
                break;
            }
        }
    }

    private static string Expand(string value, Dictionary<string, string> variables)
    {
        var result = Environment.ExpandEnvironmentVariables(value.Replace("{AppData}", App.DataFolder));
        foreach (var pair in variables)
            result = result.Replace("{" + pair.Key + "}", pair.Value, StringComparison.OrdinalIgnoreCase);
        return result;
    }

    private static string[] Split(string value) =>
        value.Split(new[] { "=>" }, 2, StringSplitOptions.TrimEntries);

    private static bool Evaluate(string condition, Dictionary<string, string> variables)
    {
        var c = Expand(condition, variables).Trim();
        if (string.IsNullOrWhiteSpace(c)) return true;
        foreach (var op in new[] { "==", "!=", ">=", "<=", ">", "<" })
        {
            var i = c.IndexOf(op, StringComparison.Ordinal);
            if (i < 0) continue;
            var left = c[..i].Trim();
            var right = c[(i + op.Length)..].Trim();
            if (double.TryParse(left, out var a) && double.TryParse(right, out var b))
                return op switch { "==" => a == b, "!=" => a != b, ">=" => a >= b, "<=" => a <= b, ">" => a > b, "<" => a < b, _ => false };
            return op switch
            {
                "==" => string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
                "!=" => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
        return true;
    }

    private static string Describe(WorkflowNode node, string parameter) =>
        string.IsNullOrWhiteSpace(parameter) ? node.Title : $"{node.Title}: {parameter}";

    public sealed class RunResult
    {
        public bool Success { get; set; } = true;
        public int CompletedSteps { get; set; }
        public string? Error { get; set; }
    }
}
