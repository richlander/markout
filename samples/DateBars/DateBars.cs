// Visualize today's date as a framed dashboard with heat-colored progress bars

using Markout;
using Markout.Ansi.Spectre;
using Spectre.Console;

var now = DateTime.Now;
var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
var daysInYear = new DateTime(now.Year, 12, 31).DayOfYear;

var icon = now.Hour switch
{
    >= 6 and < 18 => "☀️",
    _ => "🌙"
};

var view = new DateProgressView
{
    Title = $"{icon}  {now:dddd, MMMM d, yyyy — h:mm tt}",
    Progress =
    [
        new Breakdown("Month",  [new Segment("Elapsed", now.Month),     new Segment("Remaining", 12 - now.Month)]),
        new Breakdown("Day",    [new Segment("Elapsed", now.Day),       new Segment("Remaining", daysInMonth - now.Day)]),
        new Breakdown("Year",   [new Segment("Elapsed", now.DayOfYear), new Segment("Remaining", daysInYear - now.DayOfYear)]),
        new Breakdown("Hour",   [new Segment("Elapsed", now.Hour),      new Segment("Remaining", 24 - now.Hour)]),
        new Breakdown("Minute", [new Segment("Elapsed", now.Minute),    new Segment("Remaining", 60 - now.Minute)]),
    ]
};

// Serialize into a Spectre panel via a StringWriter, then frame it
var sw = new StringWriter();
MarkoutSerializer.Serialize(view, Console.Out, new SpectreWriter(AnsiConsole.Console), DateBarsContext.Default);

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class DateProgressView
{
    public string Title { get; set; } = "";

    [MarkoutIgnoreInTable]
    public IReadOnlyList<Breakdown>? Progress { get; set; }
}

[MarkoutContext(typeof(DateProgressView))]
public partial class DateBarsContext : MarkoutSerializerContext { }
