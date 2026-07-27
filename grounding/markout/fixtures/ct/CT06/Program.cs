// A service catalog produced this component record. Plain data — it carries no output formatting.

var component = new Component
{
    Name = "Auth Service",
    Summary = "Issues and validates access tokens for the platform APIs.",
    Owner = "Platform Team",
    Status = "Healthy",
};

// TODO: Using the referenced Markout library, render the report described by the eval task.
// Produce output through Markout's serializer — do not hand-write Markdown/tables. Build and run to confirm.

public sealed class Component
{
    public string Name { get; init; } = "";
    public string Summary { get; init; } = "";
    public string Owner { get; init; } = "";
    public string Status { get; init; } = "";
}
