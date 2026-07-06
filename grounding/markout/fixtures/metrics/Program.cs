// A build pipeline produced these step timings. Render a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var title = "Build Report";

var steps = new List<StepTiming>
{
    new("Restore", 1.2),
    new("Compile", 4.8),
    new("Test", 9.6),
};

// TODO: print a GitHub-flavored Markdown report to the console: an H1 title, then a
// "## Performance" section showing each step's time as a measurement using the library's
// built-in metric shape (do not hand-write the table). See task prompt.

public record StepTiming(string Name, double Seconds);
