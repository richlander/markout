namespace Markout;

/// <summary>
/// A name-kind pair representing a discoverable schema element.
/// </summary>
public readonly record struct SchemaItem(string Name, string Kind);

/// <summary>
/// Describes a section's schema: its items and their kind.
/// </summary>
public sealed class SectionSchema
{
    public SectionSchema(string name, string itemKind, string[] itemNames)
    {
        Name = name;
        ItemKind = itemKind;
        Items = itemNames.Select(n => new SchemaItem(n, itemKind)).ToArray();
    }

    public string Name { get; }
    public string ItemKind { get; }
    public SchemaItem[] Items { get; }
}

/// <summary>
/// Describes the schema of a Markout document: its sections and the items within them.
/// Used for discovery (-D), projection validation (--fields/--columns), and diagnostics.
/// </summary>
/// <remarks>
/// <para>Built manually today via <see cref="Add"/>. Will be source-generated from
/// Markout attributes in a future release.</para>
/// <para>All name lookups are case-insensitive.</para>
/// </remarks>
public sealed class DocumentSchema
{
    private readonly List<string> _sectionOrder = [];
    private readonly Dictionary<string, SectionSchema> _sections = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a section with typed items (fields, columns, tree nodes, or list items).
    /// </summary>
    public DocumentSchema Add(string sectionName, string itemKind, params string[] itemNames)
    {
        var schema = new SectionSchema(sectionName, itemKind, itemNames);
        _sections[sectionName] = schema;
        if (!_sectionOrder.Contains(sectionName, StringComparer.OrdinalIgnoreCase))
            _sectionOrder.Add(sectionName);
        return this;
    }

    /// <summary>
    /// Registers a section with no explicit items (e.g., a headless summary section).
    /// </summary>
    public DocumentSchema AddSection(string sectionName)
    {
        if (!_sections.ContainsKey(sectionName))
            _sections[sectionName] = new SectionSchema(sectionName, "section", []);
        if (!_sectionOrder.Contains(sectionName, StringComparer.OrdinalIgnoreCase))
            _sectionOrder.Add(sectionName);
        return this;
    }

    /// <summary>
    /// All section names in registration order.
    /// </summary>
    public string[] SectionNames => [.. _sectionOrder];

    /// <summary>
    /// Returns the schema for a section, or null if not registered.
    /// </summary>
    public SectionSchema? GetSection(string sectionName)
        => _sections.GetValueOrDefault(sectionName);

    // ── Discovery ──

    /// <summary>
    /// Discovers schema elements. Bare call returns sections.
    /// With a section name, returns items within that section.
    /// Returns null if the section name is not found.
    /// </summary>
    public SchemaItem[]? Discover(string? sectionName = null)
    {
        if (string.IsNullOrEmpty(sectionName))
            return _sectionOrder.Select(n => new SchemaItem(n, "section")).ToArray();

        var section = GetSection(sectionName);
        return section?.Items;
    }

    /// <summary>
    /// Resolves a section name with case-insensitive matching.
    /// Returns the canonical section name if found, null otherwise.
    /// </summary>
    public string? ResolveSection(string name)
    {
        if (_sections.TryGetValue(name, out var schema))
            return schema.Name;
        return null;
    }

    // ── Projection validation ──

    /// <summary>
    /// Validates requested field/column names against a section's schema.
    /// Returns which names are valid and which are unknown.
    /// </summary>
    public ProjectionValidation ValidateProjection(string sectionName, string[]? requestedNames)
    {
        if (requestedNames is not { Length: > 0 })
            return ProjectionValidation.Empty;

        var section = GetSection(sectionName);
        if (section == null)
            return new ProjectionValidation([], requestedNames, new Dictionary<string, string[]>());

        var knownNames = new HashSet<string>(
            section.Items.Select(i => i.Name), StringComparer.OrdinalIgnoreCase);

        var resolved = new List<string>();
        var unresolved = new List<string>();
        var suggestions = new Dictionary<string, string[]>();

        foreach (var name in requestedNames)
        {
            if (knownNames.Contains(name))
            {
                resolved.Add(name);
            }
            else
            {
                unresolved.Add(name);
                var prefixMatches = section.Items
                    .Where(i => i.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                    .Select(i => i.Name)
                    .ToArray();
                if (prefixMatches.Length > 0)
                    suggestions[name] = prefixMatches;
            }
        }

        return new ProjectionValidation([.. resolved], [.. unresolved], suggestions);
    }

    // ── Post-render diagnostics ──

    /// <summary>
    /// Compares requested names against rendered output to find names that were
    /// valid but produced no data. Uses case-insensitive substring matching.
    /// </summary>
    public static string[] DiagnoseRendered(string[]? requestedNames, string renderedOutput)
    {
        if (requestedNames is not { Length: > 0 } || string.IsNullOrEmpty(renderedOutput))
            return requestedNames ?? [];

        return requestedNames
            .Where(name => !renderedOutput.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}

/// <summary>
/// Result of validating projection names against a section schema.
/// </summary>
public sealed class ProjectionValidation
{
    public static readonly ProjectionValidation Empty = new([], [], new Dictionary<string, string[]>());

    public ProjectionValidation(string[] resolved, string[] unresolved,
        IReadOnlyDictionary<string, string[]> suggestions)
    {
        Resolved = resolved;
        Unresolved = unresolved;
        Suggestions = suggestions;
    }

    /// <summary>Names that matched the schema.</summary>
    public string[] Resolved { get; }

    /// <summary>Names that did not match any schema item.</summary>
    public string[] Unresolved { get; }

    /// <summary>Prefix-based suggestions for unresolved names, keyed by the unresolved name.</summary>
    public IReadOnlyDictionary<string, string[]> Suggestions { get; }

    public bool IsValid => Unresolved.Length == 0;
}
