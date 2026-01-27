using Markout;
using Markout.Demo;

if (args.Length == 0 || args[0] == "--list" || args[0] == "-l")
{
    Console.WriteLine("Available demos:");
    Console.WriteLine();
    foreach (var name in Demos.List())
    {
        Console.WriteLine($"  {name}");
    }
    return;
}

var demoName = args[0];
var demo = Demos.Get(demoName);

if (demo == null)
{
    Console.Error.WriteLine($"Unknown demo: {demoName}");
    Console.Error.WriteLine("Run 'markout --list' to see available demos.");
    Environment.Exit(1);
    return;
}

demo(Console.Out);
