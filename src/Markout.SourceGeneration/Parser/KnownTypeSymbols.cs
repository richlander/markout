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

    private INamedTypeSymbol? _graph;
    private bool _graphResolved;

    private INamedTypeSymbol? _mappedTextDiff;
    private bool _mappedTextDiffResolved;

    private INamedTypeSymbol? _markoutTable;
    private bool _markoutTableResolved;

    private INamedTypeSymbol? _metric;
    private bool _metricResolved;

    private INamedTypeSymbol? _description;
    private bool _descriptionResolved;

    private INamedTypeSymbol? _codeSection;
    private bool _codeSectionResolved;

    private INamedTypeSymbol? _callout;
    private bool _calloutResolved;

    private INamedTypeSymbol? _breakdown;
    private bool _breakdownResolved;

    private INamedTypeSymbol? _metricChange;
    private bool _metricChangeResolved;

    private INamedTypeSymbol? _multiSourceRow;
    private bool _multiSourceRowResolved;

    private INamedTypeSymbol? _markoutCell;
    private bool _markoutCellResolved;

    private INamedTypeSymbol? _markoutDeltaAttribute;
    private bool _markoutDeltaAttributeResolved;

    private INamedTypeSymbol? _markoutUnitAttribute;
    private bool _markoutUnitAttributeResolved;


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
    public INamedTypeSymbol? Graph => Resolve(ref _graph, ref _graphResolved, "Markout.Graph");
    public INamedTypeSymbol? MappedTextDiff => Resolve(ref _mappedTextDiff, ref _mappedTextDiffResolved, "Markout.MappedTextDiff");
    public INamedTypeSymbol? MarkoutTable => Resolve(ref _markoutTable, ref _markoutTableResolved, "Markout.MarkoutTable");
    public INamedTypeSymbol? Metric => Resolve(ref _metric, ref _metricResolved, "Markout.Metric");
    public INamedTypeSymbol? Description => Resolve(ref _description, ref _descriptionResolved, "Markout.Description");
    public INamedTypeSymbol? CodeSection => Resolve(ref _codeSection, ref _codeSectionResolved, "Markout.CodeSection");
    public INamedTypeSymbol? Callout => Resolve(ref _callout, ref _calloutResolved, "Markout.Callout");
    public INamedTypeSymbol? Breakdown => Resolve(ref _breakdown, ref _breakdownResolved, "Markout.Breakdown");
    public INamedTypeSymbol? MetricChange => Resolve(ref _metricChange, ref _metricChangeResolved, "Markout.MetricChange`1");
    public INamedTypeSymbol? MultiSourceRow => Resolve(ref _multiSourceRow, ref _multiSourceRowResolved, "Markout.MultiSourceRow");
    public INamedTypeSymbol? IMarkoutCell => Resolve(ref _markoutCell, ref _markoutCellResolved, "Markout.IMarkoutCell");
    public INamedTypeSymbol? MarkoutDeltaAttribute => Resolve(ref _markoutDeltaAttribute, ref _markoutDeltaAttributeResolved, "Markout.MarkoutDeltaAttribute");
    public INamedTypeSymbol? MarkoutUnitAttribute => Resolve(ref _markoutUnitAttribute, ref _markoutUnitAttributeResolved, "Markout.MarkoutUnitAttribute");
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
