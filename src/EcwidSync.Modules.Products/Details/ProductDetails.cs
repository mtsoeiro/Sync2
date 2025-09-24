using System;
using System.Collections.Generic;

namespace EcwidSync.Modules.Products.Details
{
    public sealed class ProductDetails
    {
        public long Id { get; set; }
        public string? Sku { get; set; }
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public bool? Enabled { get; set; }

        public bool? InStock { get; set; }
        public bool? Unlimited { get; set; }
        public double? Weight { get; set; }
        public string? Url { get; set; }

        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? Updated { get; set; }

        public string? ImageUrl { get; set; }

        public List<string> Categories { get; set; } = new();
        public List<NameValue> Attributes { get; set; } = new();
        public List<OptionItem> Options { get; set; } = new();

        public string? DescriptionHtml { get; set; }
        public string? DescriptionText { get; set; }    // HTML “limpo” para mostrar num TextBlock
    }

    public sealed class NameValue
    {
        public NameValue() { }
        public NameValue(string name, string value) { Name = name; Value = value; }
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public sealed class OptionItem
    {
        public OptionItem() { }
        public OptionItem(string name, IReadOnlyList<string> choices) { Name = name; Choices = new List<string>(choices); }
        public string Name { get; set; } = "";
        public List<string> Choices { get; set; } = new();
    }
}
