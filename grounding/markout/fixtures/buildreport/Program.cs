// A build pipeline produced these step timings and a warning. Render a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var title = "Build Report";
var warningMessage = "3 steps slower than budget";

var steps = new List<StepTiming>
{
    new("Restore", 1.2),
    new("Compile", 4.8),
    new("Test", 9.6),
};

// TODO: print a Markdown report with the title, a warning callout, and the step timings (see task prompt).

public record StepTiming(string Name, double Seconds);
