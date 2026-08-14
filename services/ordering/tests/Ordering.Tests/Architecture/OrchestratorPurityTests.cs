namespace Ordering.Tests.Architecture;

/// <summary>
/// Guards the one rule a Dapr Workflow orchestrator must never break: it may only ever await
/// *durable* tasks, and it must be deterministic across replays.
/// <para>
/// This exists because of a real, expensive outage in this saga. <c>await
/// CancellationTokenSource.CancelAsync()</c> — added purely to satisfy an analyzer that had no idea
/// it was looking at orchestrator code — awaits an ordinary <see cref="Task"/>. That hands control
/// to a continuation the workflow executor is not pumping, so the executor closed the turn having
/// collected no actions at all ("Sending 0 action(s)"), the next activity was never scheduled, and
/// a fully captured payment left its order in <c>AwaitingPayment</c> forever. It compiled cleanly,
/// raised no warnings, and passed review; the only symptom was a checkout that silently never
/// finished.
/// </para>
/// <para>
/// Mock-based orchestrator tests cannot catch that class of bug — with a substituted context there
/// is no executor counting actions, so the continuation just runs and the test passes. Scanning the
/// source is crude, but it is the check that actually fails when someone reintroduces it.
/// </para>
/// </summary>
public sealed class OrchestratorPurityTests
{
    // Each entry is a substring to search for, paired with what to do instead. Deliberately
    // literal — a fancier IL or Roslyn analysis would be more precise and far less obvious to the
    // next person who trips one of these at 2am.
    private static readonly (string Pattern, string Guidance)[] ForbiddenInOrchestrators =
    [
        ("CancelAsync(", "use the synchronous Cancel() — awaiting a non-durable Task ends the turn with no actions"),
        ("Task.Delay", "use context.CreateTimer, which is durable and survives replay"),
        ("Task.Run", "orchestrator code must stay on the workflow scheduler; move work into an activity"),
        ("DateTime.Now", "use context.CurrentUtcDateTime — replay must see the same instant"),
        ("DateTime.UtcNow", "use context.CurrentUtcDateTime — replay must see the same instant"),
        ("DateTimeOffset.Now", "use context.CurrentUtcDateTime — replay must see the same instant"),
        ("DateTimeOffset.UtcNow", "use context.CurrentUtcDateTime — replay must see the same instant"),
        ("Guid.NewGuid", "mint ids outside the orchestrator and pass them in — replay must be deterministic"),
        ("Guid.CreateVersion7", "mint ids outside the orchestrator and pass them in — replay must be deterministic"),
        ("new Random", "randomness is non-deterministic across replays"),
        ("HttpClient", "all I/O belongs in an activity, never the orchestrator"),
        (".Wait()", "blocking waits deadlock the workflow scheduler"),
        (".GetAwaiter().GetResult()", "blocking waits deadlock the workflow scheduler"),
    ];

    public static TheoryData<string> OrchestratorFiles
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var file in FindOrchestratorSourceFiles())
            {
                data.Add(file);
            }

            return data;
        }
    }

    [Fact]
    public void OrchestratorSourcesAreDiscoverable()
    {
        // If this fails the other test is silently vacuous — a scan that finds no files always
        // passes, which would be worse than having no test at all.
        FindOrchestratorSourceFiles().ShouldNotBeEmpty(
            "no Workflow<,> orchestrator sources were found to scan — the repo-root probe likely broke");
    }

    [Theory]
    [MemberData(nameof(OrchestratorFiles))]
    public void OrchestratorAwaitsOnlyDurableTasks(string path)
    {
        var offences = new List<string>();
        var lines = File.ReadAllLines(path);

        for (var i = 0; i < lines.Length; i++)
        {
            var code = StripComment(lines[i]);
            if (code.Length == 0)
            {
                continue;
            }

            foreach (var (pattern, guidance) in ForbiddenInOrchestrators)
            {
                if (code.Contains(pattern, StringComparison.Ordinal))
                {
                    offences.Add($"{Path.GetFileName(path)}({i + 1}): '{pattern}' — {guidance}");
                }
            }
        }

        offences.ShouldBeEmpty(string.Join(Environment.NewLine, offences));
    }

    // Drops the trailing comment so prose about a rule never trips the rule.
    private static string StripComment(string line)
    {
        var index = line.IndexOf("//", StringComparison.Ordinal);
        return (index >= 0 ? line[..index] : line).Trim();
    }

    private static List<string> FindOrchestratorSourceFiles()
    {
        var repoRoot = FindRepoRoot();
        if (repoRoot is null)
        {
            return [];
        }

        var workflowDirectory = Path.Combine(repoRoot, "services", "ordering", "Ordering.Workflow");
        if (!Directory.Exists(workflowDirectory))
        {
            return [];
        }

        // Only the orchestrators themselves. Activities are ordinary code and *should* do I/O.
        return Directory.GetFiles(workflowDirectory, "*.cs")
            .Where(file => File.ReadAllText(file).Contains(": Workflow<", StringComparison.Ordinal))
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToList();
    }

    private static string? FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "EventPlatform.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
