namespace Markout;

/// <summary>
/// Represents a callout/admonition block for source-generated rendering.
/// Recognized by the source generator as a property type that emits
/// <see cref="MarkoutWriter.WriteCallout"/> calls.
/// </summary>
/// <param name="Severity">The callout severity level.</param>
/// <param name="Message">The callout message text.</param>
public readonly record struct Callout(CalloutSeverity Severity, string Message);
