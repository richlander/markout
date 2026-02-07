namespace Markout.Tests;

// Partial implementation of generated TypeInfo hooks for testing
public partial class PackageWithSectionsMarkoutTypeInfo
{
    // Track whether hooks were called for test assertions
    internal static bool OnSerializingCalled { get; set; }
    internal static bool OnSerializedCalled { get; set; }
    internal static bool SkipAssembliesSection { get; set; }

    partial void OnSerializing(global::Markout.MarkoutWriter writer, PackageWithSections value)
    {
        OnSerializingCalled = true;
    }

    partial void OnSerialized(global::Markout.MarkoutWriter writer, PackageWithSections value)
    {
        OnSerializedCalled = true;
    }

    partial void OnBeforeSectionAssemblies(global::Markout.MarkoutWriter writer, PackageWithSections value, ref bool skip)
    {
        skip = SkipAssembliesSection;
    }
}
