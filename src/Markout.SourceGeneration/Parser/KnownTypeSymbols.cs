using Microsoft.CodeAnalysis;

namespace Markout.SourceGeneration.Parser;

/// <summary>
/// Caches well-known type symbol lookups to avoid repeated string comparisons.
/// </summary>
internal sealed class KnownTypeSymbols
{
    private readonly Compilation _compilation;

    // Lazy-resolved symbols
    private INamedTypeSymbol? _markoutField;
    private bool _markoutFieldResolved;

    private INamedTypeSymbol? _treeNode;
    private bool _treeNodeResolved;

    private INamedTypeSymbol? _barItem;
    private bool _barItemResolved;

    private INamedTypeSymbol? _labeledItem;
    private bool _labeledItemResolved;

    private INamedTypeSymbol? _codeSection;
    private bool _codeSectionResolved;

    private INamedTypeSymbol? _callout;
    private bool _calloutResolved;

    private INamedTypeSymbol? _distributionBar;
    private bool _distributionBarResolved;

    private INamedTypeSymbol? _dateTime;
    private bool _dateTimeResolved;

    private INamedTypeSymbol? _dateTimeOffset;
    private bool _dateTimeOffsetResolved;

    private INamedTypeSymbol? _iDictionary;
    private bool _iDictionaryResolved;

    private INamedTypeSymbol? _iEnumerable;
    private bool _iEnumerableResolved;

    private INamedTypeSymbol? _iMarkoutFormattable;
    private bool _iMarkoutFormattableResolved;

    public KnownTypeSymbols(Compilation compilation)
    {
        _compilation = compilation;
    }

    public INamedTypeSymbol? MarkoutField => Resolve(ref _markoutField, ref _markoutFieldResolved, "Markout.MarkoutField");
    public INamedTypeSymbol? TreeNode => Resolve(ref _treeNode, ref _treeNodeResolved, "Markout.TreeNode");
    public INamedTypeSymbol? BarItem => Resolve(ref _barItem, ref _barItemResolved, "Markout.BarItem");
    public INamedTypeSymbol? LabeledItem => Resolve(ref _labeledItem, ref _labeledItemResolved, "Markout.LabeledItem");
    public INamedTypeSymbol? CodeSection => Resolve(ref _codeSection, ref _codeSectionResolved, "Markout.CodeSection");
    public INamedTypeSymbol? Callout => Resolve(ref _callout, ref _calloutResolved, "Markout.Callout");
    public INamedTypeSymbol? DistributionBar => Resolve(ref _distributionBar, ref _distributionBarResolved, "Markout.DistributionBar");
    public INamedTypeSymbol? DateTime => Resolve(ref _dateTime, ref _dateTimeResolved, "System.DateTime");
    public INamedTypeSymbol? DateTimeOffset => Resolve(ref _dateTimeOffset, ref _dateTimeOffsetResolved, "System.DateTimeOffset");
    public INamedTypeSymbol? IDictionary => Resolve(ref _iDictionary, ref _iDictionaryResolved, "System.Collections.Generic.IDictionary`2");
    public INamedTypeSymbol? IEnumerable => Resolve(ref _iEnumerable, ref _iEnumerableResolved, "System.Collections.Generic.IEnumerable`1");
    public INamedTypeSymbol? IMarkoutFormattable => Resolve(ref _iMarkoutFormattable, ref _iMarkoutFormattableResolved, "Markout.IMarkoutFormattable");

    private INamedTypeSymbol? Resolve(ref INamedTypeSymbol? field, ref bool resolved, string metadataName)
    {
        if (!resolved)
        {
            field = _compilation.GetTypeByMetadataName(metadataName);
            resolved = true;
        }
        return field;
    }
}
