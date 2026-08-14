// API usage data. The command and signature are plain text; add format-neutral inline semantics
// while implementing the serializer output described by the task.

var report = new ApiReport
{
    Title = "API Commands",
    Rows =
    {
        new()
        {
            Command = "dotnet test",
            Signature = "List<T> Parse<T>(string input)",
        },
    },
};

// TODO: Render Markdown by default and typed JSON Lines for the "jsonl" argument through the
// referenced serializer. Mark Command and Signature as semantic inline code without storing raw
// Markdown backticks. JSONL must contain decoded plain text, not tags or Markdown.

public sealed class ApiReport
{
    public string Title { get; init; } = "";
    public List<ApiUsage> Rows { get; init; } = [];
}

public sealed class ApiUsage
{
    public string Command { get; init; } = "";
    public string Signature { get; init; } = "";
}
