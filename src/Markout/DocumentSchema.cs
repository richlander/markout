namespace Markout;

/// <summary>
/// A name-kind pair representing a discoverable schema element.
/// </summary>
public readonly record struct SchemaItem(string Name, string Kind)
{
    /// <summary>
    /// Creates a schema item with a stable source name.
    /// </summary>
    public SchemaItem(string name, string kind, string stableName)
        : this(name, kind)
    {
        StableName = stableName;
    }

    /// <summary>
    /// Stable source name for the item, usually the source property/member name.
    /// </summary>
    public string StableName { get; init; } = Name;

    /// <summary>
    /// Canonical machine-facing key for the item.
    /// </summary>
    public string Key => Formatting.FormatHelper.ToSnakeCase(StableName);
}

/// <summary>
/// Describes a section's schema: its items and their kind.
/// </summary>
public sealed class SectionSchema
{
    /// <summary>
    /// Creates a section schema from item names that all share the same kind.
    /// </summary>
    /// <param name="name">Display name of the section.</param>
    /// <param name="itemKind">Kind applied to every item, such as <c>field</c> or <c>column</c>.</param>
    /// <param name="itemNames">Display names of the items in the section.</param>
    public SectionSchema(string name, string itemKind, string[] itemNames)
        : this(name, itemKind, itemNames.Select(n => new SchemaItem(n, itemKind)).ToArray())
    {
    }

    /// <summary>
    /// Creates a section schema from items, overriding each item's kind with <paramref name="itemKind"/>.
    /// </summary>
    /// <param name="name">Display name of the section.</param>
    /// <param name="itemKind">Kind applied to every item, such as <c>field</c> or <c>column</c>.</param>
    /// <param name="items">The items in the section.</param>
    public SectionSchema(string name, string itemKind, SchemaItem[] items)
    {
        Name = name;
        ItemKind = itemKind;
        Items = items.Select(i => i with { Kind = itemKind }).ToArray();
    }

    /// <summary>Display name of the section.</summary>
    public string Name { get; }

    /// <summary>Canonical machine-facing key for the section, derived from <see cref="Name"/>.</summary>
    public string Key => Formatting.FormatHelper.ToSnakeCase(Name);

    /// <summary>Kind shared by every item in the section, such as <c>field</c> or <c>column</c>.</summary>
    public string ItemKind { get; }

    /// <summary>The items the section exposes for discovery and projection.</summary>
    public SchemaItem[] Items { get; }
}

/// <summary>
/// Describes the schema of a Markout document: its sections and the items within them.
/// Used for discovery (-D), projection validation (--fields/--columns), and diagnostics.
/// </summary>
/// <remarks>
/// <para>Built manually today via <see cref="Add(string, string, string[])"/>. Will be source-generated from
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
    /// Registers a section with typed items and stable item names.
    /// </summary>
    public DocumentSchema Add(string sectionName, string itemKind, params SchemaItem[] items)
    {
        var schema = new SectionSchema(sectionName, itemKind, items);
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
            _sections[sectionName] = new SectionSchema(sectionName, "section", Array.Empty<SchemaItem>());
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

        var headers = section.Items.Select(i => i.Name).ToArray();
        var headerNames = section.Items.Select(i => i.StableName).ToArray();
        var projection = MarkoutProjection.WithColumns(requestedNames);
        projection.TryResolveColumns(headers, headerNames, out var resolution);

        var unresolved = resolution.UnmatchedColumns.ToArray();
        var resolved = requestedNames
            .Where(n => !unresolved.Contains(n, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        var suggestions = new Dictionary<string, string[]>();

        foreach (var name in unresolved)
        {
            var prefixMatches = section.Items
                .Where(i =>
                    i.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase)
                    || i.StableName.StartsWith(name, StringComparison.OrdinalIgnoreCase)
                    || i.Key.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                .Select(i => i.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (prefixMatches.Length > 0)
                suggestions[name] = prefixMatches;
        }

        return new ProjectionValidation(resolved, unresolved, suggestions);
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
    /// <summary>A validation result with no resolved names, no unresolved names, and no suggestions.</summary>
    public static readonly ProjectionValidation Empty = new([], [], new Dictionary<string, string[]>());

    /// <summary>
    /// Creates a validation result.
    /// </summary>
    /// <param name="resolved">Names that matched the schema.</param>
    /// <param name="unresolved">Names that did not match any schema item.</param>
    /// <param name="suggestions">Prefix-based suggestions keyed by unresolved name.</param>
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

    /// <summary>Whether every requested name matched a schema item.</summary>
    public bool IsValid => Unresolved.Length == 0;
}
