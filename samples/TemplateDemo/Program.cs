using Markout;
using Markout.Templates;
using MarkdownTable.Formatting;

// A template is a human-authored document with {{placeholders}} for data.
// BindFields populates placeholders from a markdown field file — no C# model needed.
// See template.md and data.md alongside this file.

var basePath = AppContext.BaseDirectory;
var template = MarkoutTemplate.Load(Path.Combine(basePath, "template.md"));
template.TableOptions = new TableFormatterOptions();
template.BindFields(File.ReadAllBytes(Path.Combine(basePath, "data.md")));

// Render through MarkdownWriter (default)
Console.WriteLine("=== Markdown ===");
Console.WriteLine(template.Render());

// Render through plain-text MarkoutWriter
Console.WriteLine("=== Plain Text ===");
var plainWriter = new MarkoutWriter();
template.Render(plainWriter);
Console.WriteLine(plainWriter.ToString());

// Render WITHOUT commits (conditional section excluded)
Console.WriteLine("=== Markdown (no commits) ===");
var noCommits = MarkoutTemplate.Load(Path.Combine(basePath, "template.md"));
noCommits.TableOptions = new TableFormatterOptions();
noCommits.BindFields(File.ReadAllBytes(Path.Combine(basePath, "data.md")));
noCommits.Bind("commits", (string?)null);  // Override to exclude conditional section
Console.WriteLine(noCommits.Render());
