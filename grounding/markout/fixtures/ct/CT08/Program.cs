// A status monitor produced this snapshot. Plain data — it carries no output formatting.

var status = new ServiceStatus
{
    Service = "checkout",
    State = "Degraded",
    Uptime = "99.1%",
    Incidents = new() { new() { When = "09:12", Note = "latency spike" } },
    Metrics = new() { new() { Name = "p99", Value = "820ms" } },
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class ServiceStatus
{
    public string Service { get; init; } = "";
    public string State { get; init; } = "";
    public string Uptime { get; init; } = "";
    public List<Incident> Incidents { get; init; } = new();
    public List<Measure> Metrics { get; init; } = new();
}

public sealed class Incident { public string When { get; init; } = ""; public string Note { get; init; } = ""; }

public sealed class Measure { public string Name { get; init; } = ""; public string Value { get; init; } = ""; }
