// A team directory produced this roster. Plain data — it carries no output formatting.

var board = new Roster
{
    Team = "Payments",
    Members = new()
    {
        new() { Name = "Ada", Role = "Lead", Status = "Active" },
        new() { Name = "Grace", Role = "Engineer", Status = "On leave" },
    },
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class Roster
{
    public string Team { get; init; } = "";
    public List<Member> Members { get; init; } = new();
}

public sealed class Member
{
    public string Name { get; init; } = "";
    public string Role { get; init; } = "";
    public string Status { get; init; } = "";
}
