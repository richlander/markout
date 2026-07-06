// A docs page defines these terms. Render a Markdown report (see task).
// These are plain data objects — they carry no output formatting.

var title = "Glossary";

var terms = new List<TermDef>
{
    new("API", "Application Programming Interface"),
    new("CLI", "Command Line Interface"),
};

// TODO: print a GitHub-flavored Markdown report to the console: an H1 title, then a
// "## Terms" section rendering each entry as a term + explanation using the library's
// built-in description shape (do not hand-write the list). See task prompt.

public record TermDef(string Term, string Text);
