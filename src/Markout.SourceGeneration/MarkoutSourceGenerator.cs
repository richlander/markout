using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Markout.SourceGeneration.Parser;
using Markout.SourceGeneration.Emitter;

namespace Markout.SourceGeneration;

/// <summary>
/// Incremental source generator for Markout serialization.
/// </summary>
[Generator]
public sealed class MarkoutSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all contexts with [MarkoutContext] attributes
        var contexts = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsClassWithAttributes(node),
                transform: static (ctx, ct) => TypeParser.ParseContext(ctx, ct))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        // Generate context implementations and type infos
        context.RegisterSourceOutput(contexts, static (ctx, contextMeta) =>
        {
            // Track generated types to avoid duplicates
            var generatedTypes = new HashSet<string>();

            // Generate type infos for any types referenced by the context
            foreach (var type in contextMeta.Types)
            {
                // Report diagnostics for this type
                foreach (var diagnostic in type.Diagnostics)
                {
                    var diag = Diagnostic.Create(
                        diagnostic.Descriptor,
                        diagnostic.Location,
                        diagnostic.MessageArgs);
                    ctx.ReportDiagnostic(diag);
                }

                if (generatedTypes.Add(type.FullTypeName))
                {
                    var typeSource = SerializerEmitter.EmitTypeInfo(type);
                    var typeHintName = string.IsNullOrEmpty(type.Namespace)
                        ? $"{type.TypeName}MarkoutTypeInfo.g.cs"
                        : $"{type.Namespace}.{type.TypeName}MarkoutTypeInfo.g.cs";
                    ctx.AddSource(typeHintName, typeSource);
                }
            }

            // Generate context partial class
            var contextSource = SerializerEmitter.EmitContext(contextMeta);
            var contextHintName = string.IsNullOrEmpty(contextMeta.Namespace)
                ? $"{contextMeta.ClassName}.g.cs"
                : $"{contextMeta.Namespace}.{contextMeta.ClassName}.g.cs";
            ctx.AddSource(contextHintName, contextSource);
        });
    }

    private static bool IsClassWithAttributes(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDecl &&
               classDecl.AttributeLists.Count > 0 &&
               classDecl.Modifiers.Any(m => m.ValueText == "partial");
    }
}
