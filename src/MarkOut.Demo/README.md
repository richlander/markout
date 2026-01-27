# MarkOut Demo

A demonstration app showing MarkOut's capabilities for rendering C# objects as Markdown.

## Data Source

All demos use a single JSON file (`Data/shoes.json`) containing Altra running shoe data:
- Catalog metadata
- Shoes with features and reviews
- Inventory by size and color

Each demo projects this data differently to showcase various rendering patterns.

## Running Demos

```bash
dotnet run --project src/MarkOut.Demo
dotnet run --project src/MarkOut.Demo -- <demo-name>
```

## Demos

Demos are ordered from simplest to most complex.

### simple

**Intent:** Show basic scalar field rendering.

**Approach:** Built-in serialization of a single object with string, decimal, and bool properties.

```
# Altra Torin 7

Name: Altra Torin 7  
Id: torin-7  
Category: Road  
Price: 149.99  
InStock: yes  
```

---

### sections

**Intent:** Show how `[MarkOutSection]` creates headed sections with tables.

**Approach:** Built-in serialization. A single product with `List<Feature>` and `List<Review>` properties marked as sections, each rendering as an H2 with a table.

```
# Altra Lone Peak 8

Name: Altra Lone Peak 8  
Id: lone-peak-8  
Price: 144.99  

## Specifications

| Name | Value |
|------|-------|
| Stack Height | 25mm |
...

## Customer Reviews

| Author | Rating | Comment |
|--------|--------|---------|
| Alice | 5 | Best trail shoe I've ever worn! |
...
```

---

### list

**Intent:** Show bullet list rendering with custom formatting.

**Approach:** **Custom projection.** The source data is transformed to `List<string>` with formatted entries like `"Torin 7 ($149.99)"` or `"Olympus 6 ($179.99; backordered)"`. MarkOut renders `List<string>` as bullets.

```
## Available Models

- Torin 7 ($149.99)
- Escalante 4 ($139.99)
- Lone Peak 8 ($144.99)
- Olympus 6 ($179.99; backordered)
...
```

This demonstrates that when you need custom formatting, you project your data to strings before serialization.

---

### table

**Intent:** Show `List<T>` rendered as a table.

**Approach:** Built-in serialization. A list of products with scalar properties renders as a Markdown table with one row per item.

```
## Products

| Model | Category | Price | In Stock |
|-------|----------|-------|----------|
| Torin 7 | Road | 149.99 | yes |
| Escalante 4 | Road | 139.99 | yes |
...
```

---

### nested

**Intent:** Show subsection-per-item rendering for nested content.

**Approach:** Built-in serialization. When a `List<T>` contains items with nested `List<U>` properties, MarkOut detects this and renders each item as a subsection (H3) with its nested lists as tables (H4).

```
## Products

### Torin 7

Name: Torin 7  
Category: Road  
Price: 149.99  

#### Features

| Name | Value |
|------|-------|
| Stack Height | 28mm |
...

#### Reviews

| Author | Rating | Comment |
|--------|--------|---------|
| Alice | 5 | Great cushioning for long runs! |
...
```

---

### pivot

**Intent:** Show how to handle `List<List<T>>` patterns by pivoting data.

**Approach:** **Custom projection.** Raw inventory data might be `Size -> List<ColorQuantity>`, which is unsupported in tables. Instead, we pivot to a flat structure where each row is a size and each column is a color. MarkOut then renders it as a simple table.

```
## Inventory by Size

| Size | Black | Green | Red |
|------|-------|-------|-----|
| 8 | 12 | 8 | 5 |
| 9 | 18 | 15 | 10 |
...
```

---

### tree

**Intent:** Show hierarchical data as an ASCII tree.

**Approach:** **Custom projection** to `TreeNode`. Project data to `List<TreeNode>` where each node has a label and optional children. MarkOut's `WriteTree()` handles the connector logic (`├─`, `└─`, `│`).

```csharp
var tree = shoes.Select(s => new TreeNode(
    $"{s.Model} ({s.Category}, ${s.Price})",
    s.Reviews?.Select(r => $"\"{r.Comment}\" — {r.Author}")
));
writer.WriteTree(tree);
```

```
## Products with Reviews

├─ Torin 7 (Road, $149.99)
│  ├─ "Great cushioning for long runs!" — Alice (5★)
│  └─ "Comfortable but runs a bit warm." — Bob (4★)
├─ Escalante 4 (Road, $139.99)
│  └─ "Fast and light, perfect for racing." — Charlie (5★)
└─ Lone Peak 8 (Trail, $144.99)
   ├─ "Best trail shoe ever!" — Diana (5★)
   ...
```

This follows the same pattern as `list`: reshape data to a MarkOut-recognized type, serializer does the right thing.

---

### schema

**Intent:** Show how types map to MarkOut output via introspection.

**Approach:** Uses `GetSchemaInfo<T>()` to display type metadata. Shows how the same type renders differently as a document vs. as a table row (e.g., `List<Review>` becomes a table in document context but is ignored/unsupported in table context).

```
## ShoeTableRow

ShoeTableRow (as document)
├─ Model: string → Field
├─ Category: string → Column
├─ Price: decimal → Column
├─ InStock: bool → Column "In Stock"
```

---

## Patterns Summary

| Demo | Serialization | Key Concept |
|------|---------------|-------------|
| simple | Built-in | Scalar fields |
| sections | Built-in | `[MarkOutSection]` for headed tables |
| list | Custom projection | `List<string>` → bullets |
| table | Built-in | `List<T>` → table |
| nested | Built-in | Subsection-per-item for nested content |
| pivot | Custom projection | Flatten `List<List<T>>` to columns |
| tree | Custom projection | `List<TreeNode>` → hierarchical tree |
| schema | Introspection | `GetSchemaInfo<T>()` |

## Key Takeaways

1. **Built-in handling covers most cases:** Scalar fields, tables, sections, and nested structures work automatically with attributes.

2. **Tables always have headings:** When `List<T>` is serialized, it renders with a heading. The heading comes from:
   - `[MarkOutSection(Name = "...")]` if specified
   - Otherwise, the property name (e.g., `Products` → `## Products`)

3. **Table columns must be scalars:** Nested collections (`List<T>` properties) are skipped in table rows. Use `[MarkOutIgnoreInTable]` to silence the compile-time warning, or restructure with pivot/subsection patterns.

4. **Project data for custom formatting:** When you need specific output formats, transform to MarkOut-recognized types:
   - `List<string>` → bullet list
   - `List<TreeNode>` → hierarchical tree

5. **Pivot for `List<List<T>>`:** Nested collections are unsupported in tables. Pivot the data so nested items become columns.

6. **Use `MarkOutWriter` for full control:** For non-standard layouts, use the writer API directly.
