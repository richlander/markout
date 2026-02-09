#!/usr/bin/env dotnet run
#:package Markout@0.4.0

using Markout;

var product = new ProductView
{
    Name = "Widget Pro",
    Category = "Electronics",
    Price = 49.99m
};

MarkoutSerializer.Serialize(product, Console.Out, ProductContext.Default);

[MarkoutSerializable(TitleProperty = nameof(Name))]
public class ProductView
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
}

[MarkoutContext(typeof(ProductView))]
public partial class ProductContext : MarkoutSerializerContext { }
