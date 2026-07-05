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

var view = new DateProgress
{
    Title = $"{icon}  {now:dddd, MMMM d, yyyy — h:mm tt}",
    Progress =
    [
        new Breakdown("Month",  [new Slice("Elapsed", now.Month),     new Slice("Remaining", 12 - now.Month)]),
        new Breakdown("Day",    [new Slice("Elapsed", now.Day),       new Slice("Remaining", daysInMonth - now.Day)]),
        new Breakdown("Year",   [new Slice("Elapsed", now.DayOfYear), new Slice("Remaining", daysInYear - now.DayOfYear)]),
        new Breakdown("Hour",   [new Slice("Elapsed", now.Hour),      new Slice("Remaining", 24 - now.Hour)]),
        new Breakdown("Minute", [new Slice("Elapsed", now.Minute),    new Slice("Remaining", 60 - now.Minute)]),
    ]
};

// Serialize into a Spectre panel via a StringWriter, then frame it
var sw = new StringWriter();
MarkoutSerializer.Serialize(view, Console.Out, new SpectreFormatter(AnsiConsole.Console), DateBarsContext.Default);

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class DateProgress
{
    public string Title { get; set; } = "";

    [MarkoutIgnoreInTable]
    public IReadOnlyList<Breakdown>? Progress { get; set; }
}

[MarkoutContext(typeof(DateProgress))]
public partial class DateBarsContext : MarkoutSerializerContext { }
