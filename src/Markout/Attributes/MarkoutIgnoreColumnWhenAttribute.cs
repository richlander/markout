namespace Markout;

/// <summary>
/// Specifies that a table column should be conditionally hidden based on a static method's return value.
/// </summary>
/// <remarks>
/// Apply to a section property to dynamically hide a column when a condition method returns <c>true</c>.
/// The method must be a <c>static bool</c> method on the declaring type that accepts the section collection as its parameter.
/// Multiple attributes can be applied to conditionally hide different columns.
/// </remarks>
/// <example>
/// <code>
/// [MarkoutSection(Name = "Results")]
/// [MarkoutIgnoreColumnWhen(nameof(PatternIsUniform), "Pattern")]
/// [MarkoutIgnoreColumnWhen(nameof(SimilarityIsUniform), "Similarity")]
/// public List&lt;FindRow&gt;? Results { get; set; }
///
/// public static bool PatternIsUniform(List&lt;FindRow&gt;? rows)
///     =&gt; rows?.Select(r => r.Pattern).Distinct().Count() &lt;= 1;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public sealed class MarkoutIgnoreColumnWhenAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the static method that determines whether to hide the column.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Gets the name of the column to hide when the method returns <c>true</c>.
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Initializes a new instance with the specified method and column names.
    /// </summary>
    /// <param name="methodName">The name of a static bool method on the declaring type.</param>
    /// <param name="columnName">The name of the element property (column) to conditionally hide.</param>
    public MarkoutIgnoreColumnWhenAttribute(string methodName, string columnName)
    {
        MethodName = methodName;
        ColumnName = columnName;
    }
}
