using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using portfolio.Helpers;

namespace portfolio.Validation;

/// <summary>
/// Validates image URLs with <see cref="Patterns.ImageUrl"/>.
///   [ImageUrl]                    → the box holds exactly one URL
///   [ImageUrl(OnePerLine = true)] → a textarea, one URL per line, checked line by line
///
/// Server side: IsValid below (Regex.IsMatch on every line).
/// Browser side: AddValidation writes data-val-imageurl-* attributes that
/// wwwroot/js/validation.js reads, so the same rule runs before the form is sent.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ImageUrlAttribute : ValidationAttribute, IClientModelValidator
{
    public const int MaxLength = 500; // = varchar(500) of project_image.image_url

    public bool OnePerLine { get; set; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var text = value as string;
        if (string.IsNullOrWhiteSpace(text))
        {
            return ValidationResult.Success; // [Required] decides whether empty is allowed
        }

        var lines = text.Split('\n');
        var count = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var url = lines[i].Trim();
            if (url.Length == 0)
            {
                continue; // blank lines are ignored
            }

            count++;
            var where = OnePerLine ? $"Line {i + 1}" : "The URL";

            if (url.Length > MaxLength)
            {
                return new ValidationResult($"{where} is longer than {MaxLength} characters.");
            }

            if (!Regex.IsMatch(url, Patterns.ImageUrl, RegexOptions.IgnoreCase))
            {
                return new ValidationResult(
                    $"{where} is not a public image link: \"{TextHelper.Shorten(url, 60)}\". " +
                    "Use an https:// URL that ends in .png, .jpg, .jpeg, .gif, .webp or .svg " +
                    "(Google Drive, Facebook and OneDrive links do not work).");
            }
        }

        if (!OnePerLine && count > 1)
        {
            return new ValidationResult("Enter only one URL here.");
        }

        return ValidationResult.Success;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        Merge(context.Attributes, "data-val", "true");
        Merge(context.Attributes, "data-val-imageurl",
            ErrorMessage ?? "Use public https image links ending in .png, .jpg, .jpeg, .gif, .webp or .svg.");
        Merge(context.Attributes, "data-val-imageurl-pattern", Patterns.ImageUrl);
        Merge(context.Attributes, "data-val-imageurl-many", OnePerLine ? "true" : "false");
        Merge(context.Attributes, "data-val-imageurl-max", MaxLength.ToString());
    }

    private static void Merge(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }
}
